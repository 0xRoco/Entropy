using System.Text.Json;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Engine.Core;
using Entropy.Game;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Spatial;
using Entropy.Game.Components.Simulation;
using Entropy.Game.Components.Tags;
using Entropy.Game.Components.Vitals;
using OpenTK.Mathematics;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using Xunit;

namespace Entropy.Game.Tests;

public class GameSaveTests
{
    [Fact]
    public void CapturePreservesWalletBalance()
    {
        var world = new World();
        var player = world.Create();
        world.Set(player, new Position { Value = Vector2.Zero });
        world.Set(player, new Health { Current = 10, Max = 10 });
        world.Set(player, new Hunger { Current = 10, Max = 10 });
        world.Set(player, new Thirst { Current = 10, Max = 10 });
        world.Set(player, new Fatigue { Current = 10, Max = 10 });
        world.Set(player, new Wallet { CashCents = 4321 });

        var save = GameSave.Capture(
            world,
            player,
            seed: 1,
            elapsedMinutes: 0,
            mapId: "test");

        Assert.Equal(4321, save.CashCents);
    }

    [Fact]
    public void VersionOneSaveDataRemainsReadableWithoutTurnContext()
    {
        var world = new World();
        var player = world.Create();
        world.Set(player, new Position { Value = Vector2.Zero });
        world.Set(player, new Health { Current = 10, Max = 10 });
        world.Set(player, new Hunger { Current = 10, Max = 10 });
        world.Set(player, new Thirst { Current = 10, Max = 10 });
        world.Set(player, new Fatigue { Current = 10, Max = 10 });

        var json = JsonSerializer.Serialize(GameSave.Capture(
            world, player, seed: 7, elapsedMinutes: 12, mapId: "test"));
        var save = JsonSerializer.Deserialize<GameSaveData>(json);

        Assert.NotNull(save);
        Assert.Equal(GameSave.CurrentVersion, save.Version);
        Assert.Equal(12, save.ElapsedMinutes);
        Assert.Equal("test", save.MapId);
    }

    [Fact]
    public void RestoreRebuildsContainerContentsByDefinition()
    {
        var contentPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Entropy.Game/Content/Json"));
        var definitions = new Entropy.Simulation.DefinitionRegistry();
        definitions.LoadItems(contentPath);
        definitions.LoadWorldObjects(contentPath);
        var map = new TileMap(5, 5);
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
            map.SetTile(x, y, Tile.Floor);
        var maps = new MapGraph();
        maps.AddMap("test", map);
        var visibility = new VisibilityMap(5, 5);

        var source = CreateSaveContext(new World(), map, maps, visibility, definitions);
        var shelf = CreateShelf(source.World);
        source.World.Set(shelf, new LockState { Locked = true, RequiredKeyFlag = "pharmacy_key" });
        var scrap = EntitySpawner.CreateItem(source.World, "test", definitions.Item("scrap_metal"), 2, 1);
        ItemSystem.Transfer(source.World, scrap, shelf);
        var save = GameSave.Capture(source);

        var restored = CreateSaveContext(new World(), map, maps, visibility, definitions);
        var restoredShelf = CreateShelf(restored.World);
        restored.World.Set(restoredShelf, new LockState { Locked = false, RequiredKeyFlag = "pharmacy_key" });
        var generatedKey = EntitySpawner.CreateItem(restored.World, "test", definitions.Item("key_pharmacy"), 2, 1);
        ItemSystem.Transfer(restored.World, generatedKey, restoredShelf);
        GameSave.RestorePlayer(restored, save);

        var restoredItem = restored.World.Get<Container>(restoredShelf).Items.Single();
        Assert.Equal("scrap_metal", restored.World.Get<ItemIdentity>(restoredItem).DefinitionId);
        Assert.Equal(save.Containers![0].ItemStableIds[0], restored.World.StableId(restoredItem));
        Assert.True(restored.World.Get<LockState>(restoredShelf).Locked);
    }

    [Fact]
    public void RestoreRemapsEquippedItemByStableIdentityAndIsRepeatable()
    {
        var contentPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Entropy.Game/Content/Json"));
        var definitions = new Entropy.Simulation.DefinitionRegistry();
        definitions.LoadItems(contentPath);
        var map = new TileMap(5, 5);
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
            map.SetTile(x, y, Tile.Floor);
        var maps = new MapGraph();
        maps.AddMap("test", map);
        var visibility = new VisibilityMap(5, 5);

        var source = CreateSaveContext(new World(), map, maps, visibility, definitions);
        source.World.Set(source.Player, Container.Create());
        var weapon = EntitySpawner.CreateItem(source.World, "test", definitions.Item("iron_sword"), 1, 1);
        ItemSystem.Transfer(source.World, weapon, source.Player);
        source.World.Set(source.Player, new Equipped { Item = StableEntityReference.From(source.World, weapon) });
        var save = GameSave.Capture(source);
        var savedWeaponId = save.Inventory.Single().StableId;

        var restored = CreateSaveContext(new World(), map, maps, visibility, definitions);
        restored.World.Create(savedWeaponId!.Value);
        GameSave.RestorePlayer(restored, save);
        GameSave.RestorePlayer(restored, save);

        var equipped = restored.World.Get<Equipped>(restored.Player).Item.Resolve(restored.World);
        Assert.True(restored.World.IsAlive(equipped));
        Assert.Equal("iron_sword", restored.World.Get<ItemIdentity>(equipped).DefinitionId);
        Assert.Single(restored.World.Get<Container>(restored.Player).Items);
        Assert.NotEqual(savedWeaponId, restored.World.StableId(equipped));
    }

    [Fact]
    public void RestoreReturnsExistingNpcToSavedPosition()
    {
        var contentPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Entropy.Game/Content/Json"));
        var definitions = new Entropy.Simulation.DefinitionRegistry();
        definitions.LoadItems(contentPath);
        var map = new TileMap(5, 5);
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
            map.SetTile(x, y, Tile.Floor);
        var maps = new MapGraph();
        maps.AddMap("test", map);
        var visibility = new VisibilityMap(5, 5);
        var context = CreateSaveContext(new World(), map, maps, visibility, definitions);
        var npc = context.World.Create();
        context.World.Set(npc, new Position { Value = new Vector2(3, 2) });
        context.World.Set(npc, new Location { MapId = "test" });
        context.World.Set(npc, new Health { Current = 4, Max = 4 });

        var save = GameSave.Capture(context);
        context.World.Set(npc, new Position { Value = new Vector2(1, 4) });

        GameSave.RestorePlayer(context, save);

        Assert.Equal(new Vector2(3, 2), context.World.Get<Position>(npc).Value);
    }

    [Fact]
    public void RestorePreservesDoorState()
    {
        var contentPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Entropy.Game/Content/Json"));
        var definitions = new Entropy.Simulation.DefinitionRegistry();
        definitions.LoadItems(contentPath);
        var map = new TileMap(5, 5);
        var maps = new MapGraph();
        maps.AddMap("test", map);
        var visibility = new VisibilityMap(5, 5);
        var context = CreateSaveContext(new World(), map, maps, visibility, definitions);
        var key = new DoorKey("test", new Vector2i(1, 1), "test", new Vector2i(2, 1));
        context.DoorStates[key] = new DoorState { Locked = false, Broken = true, TrespassReported = true };

        var save = GameSave.Capture(context);
        context.DoorStates[key] = new DoorState { Locked = true };
        GameSave.RestorePlayer(context, save);

        Assert.Equal(new DoorState { Locked = false, Broken = true, TrespassReported = true },
            context.DoorStates[key]);
    }

    [Fact]
    public void RestorePreservesSearchedContainerAndDroppedItem()
    {
        var contentPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Entropy.Game/Content/Json"));
        var definitions = new Entropy.Simulation.DefinitionRegistry();
        definitions.LoadItems(contentPath);
        var map = new TileMap(5, 5);
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
            map.SetTile(x, y, Tile.Floor);
        var maps = new MapGraph();
        maps.AddMap("test", map);
        var visibility = new VisibilityMap(5, 5);
        var context = CreateSaveContext(new World(), map, maps, visibility, definitions);
        var shelf = CreateShelf(context.World);
        context.World.Set(shelf, new Searched());
        var dropped = EntitySpawner.CreateItem(context.World, "test", definitions.Item("scrap_metal"), 3, 3);

        var save = GameSave.Capture(context);
        context.World.Remove<Searched>(shelf);
        context.World.Destroy(dropped);
        GameSave.RestorePlayer(context, save);

        Assert.True(context.World.Has<Searched>(shelf));
        Assert.Contains(context.World.Query<ItemIdentity>(), item =>
            context.World.Get<ItemIdentity>(item).DefinitionId == "scrap_metal");
    }

    [Fact]
    public void RestorePreservesActivePlayerActivity()
    {
        var contentPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../Entropy.Game/Content/Json"));
        var definitions = new Entropy.Simulation.DefinitionRegistry();
        definitions.LoadItems(contentPath);
        var map = new TileMap(5, 5);
        var maps = new MapGraph();
        maps.AddMap("test", map);
        var visibility = new VisibilityMap(5, 5);
        var context = CreateSaveContext(new World(), map, maps, visibility, definitions);
        context.World.Set(context.Player, new Activity
        {
            Kind = ActivityKind.Sleep,
            RemainingMinutes = 30,
            TotalMinutes = 60
        });

        var save = GameSave.Capture(context);
        context.World.Remove<Activity>(context.Player);
        GameSave.RestorePlayer(context, save);

        var activity = context.World.Get<Activity>(context.Player);
        Assert.Equal(ActivityKind.Sleep, activity.Kind);
        Assert.Equal(30, activity.RemainingMinutes);
        Assert.Equal(60, activity.TotalMinutes);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{}")]
    public void ReadRejectsMalformedOrIncompleteSave(string contents)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"entropy-save-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, contents);

            Assert.Null(GameSave.Read(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static GameContext CreateSaveContext(
        World world,
        TileMap map,
        MapGraph maps,
        VisibilityMap visibility,
        Entropy.Simulation.DefinitionRegistry definitions)
    {
        var player = world.Create();
        world.Set(player, new Position { Value = new Vector2(1, 1) });
        world.Set(player, new Location { MapId = "test" });
        world.Set(player, new Health { Current = 10, Max = 10 });
        world.Set(player, new Hunger { Current = 10, Max = 10 });
        world.Set(player, new Thirst { Current = 10, Max = 10 });
        world.Set(player, new Fatigue { Current = 10, Max = 10 });
        return new GameContext
        {
            Map = map,
            MapId = "test",
            Maps = maps,
            Log = new MessageLog(),
            World = world,
            Definitions = definitions,
            Rng = new Entropy.Engine.Core.Rng(1),
            Player = player,
            Clock = new WorldClock(2001, 3, 12, 7, 30),
             Scheduler = new ActorScheduler(),
            Visibilities = new Dictionary<string, VisibilityMap> { ["test"] = visibility },
            Visibility = visibility,
            ViewRadius = 6,
            DoorDefinitions = new(),
            DoorStates = new()
        };
    }

    private static Entity CreateShelf(World world)
    {
        var shelf = world.Create();
        world.Set(shelf, new Position { Value = new Vector2(2, 1) });
        world.Set(shelf, new Location { MapId = "test" });
        world.Set(shelf, Container.WithSlots(6));
        world.Set(shelf, new WorldObjectIdentity { DefinitionId = "store_shelf", Name = "Shelf" });
        return shelf;
    }
}

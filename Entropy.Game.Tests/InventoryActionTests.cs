using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.ItemEffects;
using Entropy.Game.Components.Vitals;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using OpenTK.Mathematics;
using Xunit;

namespace Entropy.Game.Tests;

public class InventoryActionTests
{
    [Fact]
    public void SuccessfulHealingConsumesItemAndTurn()
    {
        var fixture = CreateFixture(5);
        var item = fixture.CreateItem("bandage");
        fixture.World.Set(item, new Healing { Amount = 3 });
        fixture.PutInInventory(item);

        var result = ItemActions.AvailableFor(fixture.Context, item)
            .Single(action => action.Label == "Use")
            .Execute(fixture.Context, fixture.Player, item);

        Assert.Equal(ActionResult.Turn, result);
        Assert.Equal(8, fixture.World.Get<Health>(fixture.Player).Current);
        Assert.False(fixture.World.IsAlive(item));
    }

    [Fact]
    public void FailedHealingDoesNotConsumeItemOrTurn()
    {
        var fixture = CreateFixture(10);
        var item = fixture.CreateItem("bandage");
        fixture.World.Set(item, new Healing { Amount = 3 });
        fixture.PutInInventory(item);

        var result = ItemActions.AvailableFor(fixture.Context, item)
            .Single(action => action.Label == "Use")
            .Execute(fixture.Context, fixture.Player, item);

        Assert.Equal(ActionResult.Failed, result);
        Assert.True(fixture.World.IsAlive(item));
        Assert.Equal(10, fixture.World.Get<Health>(fixture.Player).Current);
    }

    [Fact]
    public void WieldIsFreeAndDropConsumesTurn()
    {
        var fixture = CreateFixture(10);
        var item = fixture.CreateItem("bat");
        fixture.World.Set(item, new Damage { Amount = 4 });
        fixture.PutInInventory(item);

        var actions = ItemActions.AvailableFor(fixture.Context, item);
        var wield = actions.Single(action => action.Label == "Wield").Execute(fixture.Context, fixture.Player, item);
        var drop = actions.Single(action => action.Label == "Drop").Execute(fixture.Context, fixture.Player, item);

        Assert.Equal(ActionResult.Free, wield);
        Assert.Equal(ActionResult.Turn, drop);
        Assert.True(fixture.World.Has<Position>(item));
        Assert.False(fixture.World.Has<InContainer>(item));
    }

    [Fact]
    public void StackableUseDecrementsStackWithoutDestroyingIt()
    {
        var fixture = CreateFixture(5);
        var item = fixture.CreateItem("food");
        fixture.World.Set(item, new Nutrition { Amount = 2 });
        fixture.World.Set(item, new Stackable { Count = 2, MaxStack = 5 });
        fixture.PutInInventory(item);

        var result = ItemActions.AvailableFor(fixture.Context, item)
            .Single(action => action.Label == "Eat")
            .Execute(fixture.Context, fixture.Player, item);

        Assert.Equal(ActionResult.Turn, result);
        Assert.True(fixture.World.IsAlive(item));
        Assert.Equal(1, fixture.World.Get<Stackable>(item).Count);
    }

    [Fact]
    public void FullInventoryStillAcceptsItemThatFitsExistingStack()
    {
        var fixture = CreateFixture(5);
        fixture.World.Set(fixture.Player, Container.WithSlots(1));

        var existing = fixture.CreateItem("food");
        fixture.World.Set(existing, new Stackable { Count = 4, MaxStack = 5 });
        fixture.PutInInventory(existing);

        var incoming = fixture.CreateItem("food");
        fixture.World.Set(incoming, new Stackable { Count = 1, MaxStack = 5 });

        Assert.True(ItemSystem.Transfer(fixture.World, incoming, fixture.Player));
        Assert.Equal(5, fixture.World.Get<Stackable>(existing).Count);
        Assert.False(fixture.World.IsAlive(incoming));
    }

    private static Fixture CreateFixture(int health)
    {
        var map = new TileMap(3, 3);
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
            map.SetTile(x, y, Tile.Floor);

        var maps = new MapGraph();
        maps.AddMap("test", map);
        var world = new World();
        var player = world.Create();
        world.Set(player, new Position { Value = new Vector2(1, 1) });
        world.Set(player, new Location { MapId = "test" });
        world.Set(player, new Health { Current = health, Max = 10 });
        world.Set(player, new Hunger { Current = 0, Max = 10 });
        world.Set(player, new Thirst { Current = 0, Max = 10 });
        world.Set(player, Container.Create());

        var visibility = new VisibilityMap(3, 3);
        var turns = new TurnProcessor();
        var context = new GameContext
        {
            Map = map,
            MapId = "test",
            Maps = maps,
            Log = new MessageLog(),
            World = world,
            Definitions = new DefinitionRegistry(),
            Rng = new Rng(1234),
            Player = player,
            Clock = new WorldClock(2001, 3, 12, 7, 30),
             Scheduler = turns.Scheduler,
            Visibilities = new Dictionary<string, VisibilityMap> { ["test"] = visibility },
            Visibility = visibility,
            ViewRadius = 6,
            DoorDefinitions = new(),
            DoorStates = new()
        };

        return new Fixture(world, player, context);
    }

    private sealed class Fixture(World world, Entity player, GameContext context)
    {
        public World World { get; } = world;
        public Entity Player { get; } = player;
        public GameContext Context { get; } = context;

        public Entity CreateItem(string name)
        {
            var item = World.Create();
            World.Set(item, new Item());
            World.Set(item, new ItemIdentity { DefinitionId = name, Name = name });
            return item;
        }

        public void PutInInventory(Entity item)
        {
            Assert.True(ItemSystem.Transfer(World, item, Player));
        }
    }
}

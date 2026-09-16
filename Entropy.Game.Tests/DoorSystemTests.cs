using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.Spatial;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using OpenTK.Mathematics;
using Xunit;

namespace Entropy.Game.Tests;

public class DoorSystemTests
{
    [Fact]
    public void LockedDoorIsNotPassable()
    {
        var fixture = CreateFixture();

        Assert.False(DoorSystem.IsPassable(fixture.Context, fixture.Transition));
    }

    [Fact]
    public void MovementUsesDoorStateAndFailsWhileLocked()
    {
        var fixture = CreateFixture();

        var result = PlayerActions.Move(
            fixture.Player, new Vector2i(1, 0), fixture.Context, fixture.Visibility, 6, fixture.Turns.Scheduler);

        Assert.Equal(ActionResult.Failed, result);
        Assert.Equal(new Vector2(0, 1), fixture.World.Get<Position>(fixture.Player).Value);
    }

    [Fact]
    public void CorrectKeyUnlocksDoor()
    {
        var fixture = CreateFixture();
        fixture.PutInInventory(fixture.CreateItem("key_pharmacy"));

        var result = DoorSystem.Unlock(fixture.Context, fixture.Player, fixture.Transition);

        Assert.Equal(ActionResult.Turn, result);
        Assert.True(DoorSystem.IsPassable(fixture.Context, fixture.Transition));
    }

    [Fact]
    public void MissingKeyCannotUnlockDoor()
    {
        var fixture = CreateFixture();

        var result = DoorSystem.Unlock(fixture.Context, fixture.Player, fixture.Transition);

        Assert.Equal(ActionResult.Failed, result);
        Assert.False(DoorSystem.IsPassable(fixture.Context, fixture.Transition));
    }

    [Fact]
    public void ForceEntryBreaksDoorAndEmitsNoise()
    {
        var fixture = CreateFixture();
        fixture.PutInInventory(fixture.CreateItem("baseball_bat"));

        var result = DoorSystem.ForceEntry(
            fixture.Context, fixture.Player, fixture.Transition, fixture.Transition.FromTile);

        Assert.Equal(ActionResult.Turn, result);
        Assert.True(DoorSystem.IsPassable(fixture.Context, fixture.Transition));
        Assert.Contains(fixture.Context.Events.History,
            simulationEvent => simulationEvent.Description.Contains("noise carries", StringComparison.Ordinal));
    }

    [Fact]
    public void MissingForceToolLeavesDoorUnchangedAndSilent()
    {
        var fixture = CreateFixture();

        var result = DoorSystem.ForceEntry(
            fixture.Context, fixture.Player, fixture.Transition, fixture.Transition.FromTile);

        Assert.Equal(ActionResult.Failed, result);
        Assert.False(DoorSystem.IsPassable(fixture.Context, fixture.Transition));
        Assert.DoesNotContain(fixture.Context.Log.Messages,
            message => message.Text.Contains("noise", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BrokenDoorRemainsPassable()
    {
        var fixture = CreateFixture();
        var key = DoorSystem.KeyFor(fixture.Transition);
        fixture.Context.DoorStates[key] = new DoorState { Locked = true, Broken = true };

        Assert.True(DoorSystem.IsPassable(fixture.Context, fixture.Transition));
    }

    [Fact]
    public void LockedContainerRequiresUnlockBeforeSearch()
    {
        var fixture = CreateFixture();
        fixture.Context.Definitions.LoadWorldObjects(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "../../../../Entropy.Game/Content/Json")));
        var container = fixture.CreateContainer();
        fixture.World.Set(container, new LockState
        {
            Locked = true,
            RequiredKeyFlag = "key_pharmacy",
            RequiredToolFlag = "tool_crowbar"
        });

        Assert.Equal(ActionResult.Failed, SearchSystem.Start(fixture.Context, fixture.Player, container));
        fixture.PutInInventory(fixture.CreateItem("key_pharmacy"));
        Assert.Equal(ActionResult.Turn, DoorSystem.UnlockContainer(fixture.Context, fixture.Player, container));
        Assert.Equal(ActionResult.Turn, SearchSystem.Start(fixture.Context, fixture.Player, container));
    }

    [Fact]
    public void CrowbarForcesContainerOpenWithLessNoiseThanBashing()
    {
        var fixture = CreateFixture();
        fixture.Context.Definitions.LoadWorldObjects(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "../../../../Entropy.Game/Content/Json")));
        var container = fixture.CreateContainer();
        fixture.World.Set(container, new LockState
        {
            Locked = true,
            RequiredKeyFlag = "key_pharmacy",
            RequiredToolFlag = "tool_crowbar"
        });
        fixture.PutInInventory(fixture.CreateItem("crowbar"));

        Assert.Equal(ActionResult.Turn, DoorSystem.ForceContainerEntry(
            fixture.Context, fixture.Player, container, new Vector2i(1, 1),
            "tool_crowbar", "pry", 8));
        Assert.True(fixture.World.Get<LockState>(container).Broken);
        Assert.Contains(fixture.Context.Events.History,
            simulationEvent => simulationEvent.Description.Contains("pry the lock", StringComparison.Ordinal));
    }

    private static Fixture CreateFixture()
    {
        var from = new TileMap(3, 3);
        var to = new TileMap(3, 3);
        for (var y = 0; y < from.Height; y++)
        for (var x = 0; x < from.Width; x++)
        {
            from.SetTile(x, y, Tile.Floor);
            to.SetTile(x, y, Tile.Floor);
        }

        var maps = new MapGraph();
        maps.AddMap("from", from);
        maps.AddMap("to", to);
        maps.Connect("from", new Vector2i(1, 1), "to", new Vector2i(0, 1));
        var transition = maps.TransitionAt("from", new Vector2i(1, 1))!;

        var world = new World();
        var player = world.Create();
        world.Set(player, new Position { Value = new Vector2(0, 1) });
        world.Set(player, new Location { MapId = "from" });
        world.Set(player, new Container { Items = [], Slots = 10 });

        var visibility = new VisibilityMap(3, 3);
        var turns = new TurnProcessor();
        turns.AddActor(player);
        var definitions = new DefinitionRegistry();
        definitions.LoadItems(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "../../../../Entropy.Game/Content/Json")));
        var key = DoorSystem.KeyFor(transition);
        var context = new GameContext
        {
            Map = from,
            MapId = "from",
            Maps = maps,
            Log = new MessageLog(),
            World = world,
            Definitions = definitions,
            Rng = new Rng(1234),
            Player = player,
            Clock = new WorldClock(2001, 3, 12, 7, 30),
             Scheduler = turns.Scheduler,
            Visibilities = new Dictionary<string, VisibilityMap> { ["from"] = visibility },
            Visibility = visibility,
            ViewRadius = 6,
            DoorDefinitions = new()
            {
                [key] = new DoorDefinition("test_door", "key_pharmacy", "tool_smash")
            },
            DoorStates = new()
            {
                [key] = new DoorState { Locked = true }
            }
        };

        return new Fixture(world, player, transition, from, visibility, turns, context);
    }

    private sealed class Fixture(
        World world,
        Entity player,
        MapTransition transition,
        TileMap map,
        VisibilityMap visibility,
        TurnProcessor turns,
        GameContext context)
    {
        public World World { get; } = world;
        public Entity Player { get; } = player;
        public MapTransition Transition { get; } = transition;
        public TileMap Map { get; } = map;
        public VisibilityMap Visibility { get; } = visibility;
        public TurnProcessor Turns { get; } = turns;
        public GameContext Context { get; } = context;

        public Entity CreateItem(string definitionId)
        {
            var item = World.Create();
            World.Set(item, new Item());
            World.Set(item, new ItemIdentity { DefinitionId = definitionId, Name = definitionId });
            return item;
        }

        public Entity CreateContainer()
        {
            var container = World.Create();
            World.Set(container, Container.WithSlots(4));
            return container;
        }

        public void PutInInventory(Entity item) =>
            Assert.True(ItemSystem.Transfer(World, item, Player));
    }
}

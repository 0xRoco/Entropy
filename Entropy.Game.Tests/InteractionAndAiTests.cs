using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.Tags;
using Entropy.Game.Components.Vitals;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using OpenTK.Mathematics;
using Xunit;

namespace Entropy.Game.Tests;

public class InteractionAndAiTests
{
    [Fact]
    public void AttackDamagesLivingTargetAndReturnsTurn()
    {
        var fixture = CreateFixture();
        var attacker = fixture.CreateActor("attacker", new Vector2(1, 1), health: 10);
        var target = fixture.CreateActor("target", new Vector2(1, 2), health: 5);

        var result = InteractionSystem.Attack(fixture.Context, attacker, target);

        Assert.Equal(ActionResult.Turn, result);
        Assert.Equal(4, fixture.World.Get<Health>(target).Current);
        Assert.True(fixture.World.IsAlive(target));
    }

    [Fact]
    public void AttackDestroysTargetWhenHealthReachesZero()
    {
        var fixture = CreateFixture();
        var attacker = fixture.CreateActor("attacker", new Vector2(1, 1), health: 10);
        var target = fixture.CreateActor("target", new Vector2(1, 2), health: 1);
        fixture.Turns.AddActor(target);

        var result = InteractionSystem.Attack(fixture.Context, attacker, target);

        Assert.Equal(ActionResult.Turn, result);
        Assert.False(fixture.World.IsAlive(target));
    }

    [Fact]
    public void AttackAgainstDeadTargetFailsSafely()
    {
        var fixture = CreateFixture();
        var attacker = fixture.CreateActor("attacker", new Vector2(1, 1), health: 10);
        var target = fixture.CreateActor("target", new Vector2(1, 2), health: 1);
        fixture.World.Destroy(target);

        var result = InteractionSystem.Attack(fixture.Context, attacker, target);

        Assert.Equal(ActionResult.Failed, result);
    }

    [Fact]
    public void TransferRejectsSelfTransferAndLeavesContainerUnchanged()
    {
        var fixture = CreateFixture();
        fixture.World.Set(fixture.Player, Container.WithSlots(2));

        var result = ItemSystem.Transfer(fixture.World, fixture.Player, fixture.Player);

        Assert.False(result);
        Assert.Empty(fixture.World.Get<Container>(fixture.Player).Items);
    }

    [Fact]
    public void TransferRejectsContainmentCycle()
    {
        var fixture = CreateFixture();
        var outer = fixture.CreateContainer();
        var inner = fixture.CreateContainer();
        Assert.True(ItemSystem.Transfer(fixture.World, inner, outer));

        var result = ItemSystem.Transfer(fixture.World, outer, inner);

        Assert.False(result);
        Assert.Contains(inner, fixture.World.Get<Container>(outer).Items);
        Assert.DoesNotContain(outer, fixture.World.Get<Container>(inner).Items);
    }

    [Fact]
    public void TransferRejectsFullContainer()
    {
        var fixture = CreateFixture();
        var container = fixture.CreateContainer(slots: 1);
        var first = fixture.CreateItem("first");
        var second = fixture.CreateItem("second");
        Assert.True(ItemSystem.Transfer(fixture.World, first, container));

        var result = ItemSystem.Transfer(fixture.World, second, container);

        Assert.False(result);
        Assert.Contains(first, fixture.World.Get<Container>(container).Items);
        Assert.False(fixture.World.Has<InContainer>(second));
    }

    [Fact]
    public void AiStepTowardMovesToNextWalkableTile()
    {
        var fixture = CreateFixture();
        var actor = fixture.CreateActor("npc", new Vector2(1, 1), health: 10);

        var moved = AiUtil.StepToward(
            fixture.World, actor, fixture.Map, new Vector2i(3, 1));

        Assert.True(moved);
        Assert.Equal(new Vector2(2, 1), fixture.World.Get<Position>(actor).Value);
    }

    [Fact]
    public void AiStepTowardFailsWhenGoalIsBlocked()
    {
        var fixture = CreateFixture();
        fixture.Map.SetTile(2, 1, Tile.Wall);
        fixture.Map.SetTile(3, 1, Tile.Wall);
        var actor = fixture.CreateActor("npc", new Vector2(1, 1), health: 10);

        var moved = AiUtil.StepToward(
            fixture.World, actor, fixture.Map, new Vector2i(3, 1));

        Assert.False(moved);
        Assert.Equal(new Vector2(1, 1), fixture.World.Get<Position>(actor).Value);
    }

    private static Fixture CreateFixture()
    {
        var map = new TileMap(4, 3);
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
            map.SetTile(x, y, Tile.Floor);

        var maps = new MapGraph();
        maps.AddMap("test", map);
        var world = new World();
        var player = world.Create();
        world.Set(player, new Position { Value = new Vector2(0, 0) });
        world.Set(player, new Location { MapId = "test" });

        var visibility = new VisibilityMap(map.Width, map.Height);
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

        return new Fixture(map, world, player, turns, context);
    }

    private sealed class Fixture(TileMap map, World world, Entity player, TurnProcessor turns, GameContext context)
    {
        public TileMap Map { get; } = map;
        public World World { get; } = world;
        public Entity Player { get; } = player;
        public TurnProcessor Turns { get; } = turns;
        public GameContext Context { get; } = context;

        public Entity CreateActor(string name, Vector2 position, int health)
        {
            var actor = World.Create();
            World.Set(actor, new Actor());
            World.Set(actor, new Position { Value = position });
            World.Set(actor, new Location { MapId = "test" });
            World.Set(actor, new Health { Current = health, Max = health });
            World.Set(actor, new CreatureIdentity { DefinitionId = name });
            World.Set(actor, new Named { Name = name });
            return actor;
        }

        public Entity CreateContainer(int slots = 2)
        {
            var container = World.Create();
            World.Set(container, Container.WithSlots(slots));
            return container;
        }

        public Entity CreateItem(string name)
        {
            var item = World.Create();
            World.Set(item, new Item());
            World.Set(item, new ItemIdentity { DefinitionId = name, Name = name });
            return item;
        }
    }
}

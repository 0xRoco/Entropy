using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components.Simulation;
using Entropy.Game.Components.Vitals;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using OpenTK.Mathematics;
using Xunit;

namespace Entropy.Game.Tests;

public class ActivityTests
{
    [Fact]
    public void StartingSleepCreatesActivityAndConsumesTurn()
    {
        var fixture = CreateFixture();
        var bed = fixture.World.Create();

        var result = SleepSystem.FallAsleep(fixture.Context, fixture.Player, bed);

        Assert.Equal(ActionResult.Turn, result);
        Assert.True(fixture.World.Has<Activity>(fixture.Player));
        Assert.Equal(ActivityKind.Sleep, fixture.World.Get<Activity>(fixture.Player).Kind);
        Assert.Equal(480, fixture.World.Get<Activity>(fixture.Player).RemainingMinutes);
    }

    [Fact]
    public void ActivityProgressReducesRemainingTimeAndRecoversFatigue()
    {
        var fixture = CreateFixture();
        var started = ActivitySystem.Start(fixture.Context, fixture.Player, new Activity
        {
            Kind = ActivityKind.Sleep,
            RemainingMinutes = 10,
            TotalMinutes = 10
        });
        Assert.True(started);

        var tick = ActivitySystem.Advance(fixture.Context, fixture.Player);

        Assert.Equal(ActivityState.InProgress, tick.State);
        Assert.Equal(9, tick.RemainingMinutes);
        Assert.Equal(3, fixture.World.Get<Fatigue>(fixture.Player).Current);
    }

    [Fact]
    public void ActivityCompletesAndRemovesComponent()
    {
        var fixture = CreateFixture();
        Assert.True(ActivitySystem.Start(fixture.Context, fixture.Player, new Activity
        {
            Kind = ActivityKind.Sleep,
            RemainingMinutes = 1,
            TotalMinutes = 1
        }));

        var tick = ActivitySystem.Advance(fixture.Context, fixture.Player);

        Assert.Equal(ActivityState.Completed, tick.State);
        Assert.False(fixture.World.Has<Activity>(fixture.Player));
    }

    [Fact]
    public void HungerInterruptsSleep()
    {
        var fixture = CreateFixture();
        fixture.World.Get<Hunger>(fixture.Player).Current = 0;
        Assert.True(ActivitySystem.Start(fixture.Context, fixture.Player, new Activity
        {
            Kind = ActivityKind.Sleep,
            RemainingMinutes = 10,
            TotalMinutes = 10
        }));

        var tick = ActivitySystem.Advance(fixture.Context, fixture.Player);

        Assert.Equal(ActivityState.Interrupted, tick.State);
        Assert.False(fixture.World.Has<Activity>(fixture.Player));
    }

    [Fact]
    public void CancelRemovesActivityAndReportsCancellation()
    {
        var fixture = CreateFixture();
        Assert.True(ActivitySystem.Start(fixture.Context, fixture.Player, new Activity
        {
            Kind = ActivityKind.Sleep,
            RemainingMinutes = 10,
            TotalMinutes = 10
        }));

        var result = ActivitySystem.Cancel(fixture.Context, fixture.Player, "You stop.");

        Assert.Equal(ActivityState.Cancelled, result.State);
        Assert.False(fixture.World.Has<Activity>(fixture.Player));
        Assert.Contains(fixture.Context.Events.Drain(), simulationEvent => simulationEvent.Description == "You stop.");
    }

    private static Fixture CreateFixture()
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
        world.Set(player, new Health { Current = 10, Max = 10 });
        world.Set(player, new Hunger { Current = 10, Max = 10 });
        world.Set(player, new Thirst { Current = 10, Max = 10 });
        world.Set(player, new Fatigue { Current = 0, Max = 10 });

        var visibility = new VisibilityMap(3, 3);
        return new Fixture(world, player, new GameContext
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
             Scheduler = new ActorScheduler(),
            Visibilities = new Dictionary<string, VisibilityMap> { ["test"] = visibility },
            Visibility = visibility,
            ViewRadius = 6,
            DoorDefinitions = new(),
            DoorStates = new()
        });
    }

    private sealed record Fixture(World World, Entity Player, GameContext Context);
}

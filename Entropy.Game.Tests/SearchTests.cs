using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.Simulation;
using Entropy.Game.Components.Tags;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using OpenTK.Mathematics;
using Xunit;

namespace Entropy.Game.Tests;

public class SearchTests
{
    [Fact]
    public void StartingSearchCreatesActivityWithoutRevealingContainer()
    {
        var fixture = CreateFixture();

        var result = SearchSystem.Start(fixture.Context, fixture.Player, fixture.Container);

        Assert.Equal(ActionResult.Turn, result);
        Assert.True(fixture.World.Has<Activity>(fixture.Player));
        Assert.False(fixture.World.Has<Searched>(fixture.Container));
        Assert.Equal(SearchSystem.DurationMinutes,
            fixture.World.Get<Activity>(fixture.Player).RemainingMinutes);
    }

    [Fact]
    public void SearchProgressDoesNotMarkContainerSearched()
    {
        var fixture = CreateFixture();
        Assert.Equal(ActionResult.Turn,
            SearchSystem.Start(fixture.Context, fixture.Player, fixture.Container));

        var tick = ActivitySystem.Advance(fixture.Context, fixture.Player, 4);

        Assert.Equal(ActivityState.InProgress, tick.State);
        Assert.Equal(1, tick.RemainingMinutes);
        Assert.False(fixture.World.Has<Searched>(fixture.Container));
    }

    [Fact]
    public void CompletingSearchMarksContainerSearched()
    {
        var fixture = CreateFixture();
        Assert.Equal(ActionResult.Turn,
            SearchSystem.Start(fixture.Context, fixture.Player, fixture.Container));

        var tick = ActivitySystem.Advance(
            fixture.Context, fixture.Player, SearchSystem.DurationMinutes);

        Assert.Equal(ActivityState.Completed, tick.State);
        Assert.False(fixture.World.Has<Activity>(fixture.Player));
        Assert.True(fixture.World.Has<Searched>(fixture.Container));
        Assert.DoesNotContain(fixture.Context.Log.Messages,
            message => message.Text == "You wake up feeling rested.");
    }

    [Fact]
    public void DestroyedSearchTargetInterruptsActivity()
    {
        var fixture = CreateFixture();
        Assert.Equal(ActionResult.Turn,
            SearchSystem.Start(fixture.Context, fixture.Player, fixture.Container));
        fixture.World.Destroy(fixture.Container);

        var tick = ActivitySystem.Advance(fixture.Context, fixture.Player);

        Assert.Equal(ActivityState.Interrupted, tick.State);
        Assert.False(fixture.World.Has<Activity>(fixture.Player));
    }

    [Fact]
    public void AlreadySearchedContainerCannotStartAnotherSearch()
    {
        var fixture = CreateFixture();
        fixture.World.Set(fixture.Container, new Searched());

        var result = SearchSystem.Start(fixture.Context, fixture.Player, fixture.Container);

        Assert.Equal(ActionResult.Failed, result);
        Assert.False(fixture.World.Has<Activity>(fixture.Player));
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
        world.Set(player, new Entropy.Game.Components.Vitals.Health { Current = 10, Max = 10 });
        world.Set(player, new Entropy.Game.Components.Vitals.Hunger { Current = 10, Max = 10 });
        world.Set(player, new Entropy.Game.Components.Vitals.Thirst { Current = 10, Max = 10 });
        world.Set(player, new Entropy.Game.Components.Vitals.Fatigue { Current = 0, Max = 10 });

        var container = world.Create();
        world.Set(container, Container.Create());
        world.Set(container, new WorldObjectIdentity { DefinitionId = "chest", Name = "chest" });

        var visibility = new VisibilityMap(3, 3);
        return new Fixture(world, player, container, new GameContext
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

    private sealed record Fixture(World World, Entity Player, Entity Container, GameContext Context);
}

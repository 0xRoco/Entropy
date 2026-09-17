using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components.Tags;
using Entropy.Game.Components.Vitals;
using Entropy.Game.Systems;
using OpenTK.Mathematics;
using Xunit;

namespace Entropy.Game.Tests;

public class TurnProcessorTests
{
    [Fact]
    public void WalkableMoveReturnsTurnAction()
    {
        var fixture = CreateFixture();

        var result = PlayerActions.Move(
            fixture.Player, new Vector2i(1, 0), fixture.Context, fixture.Visibility, 6, fixture.Turns.Scheduler);

        Assert.True(result.AdvancesTime);
        Assert.Equal(1, result.TimeCostMinutes);
        Assert.Equal(new Vector2(2, 1), fixture.World.Get<Position>(fixture.Player).Value);
    }

    [Fact]
    public void BlockedMoveReturnsFailedAction()
    {
        var fixture = CreateFixture();
        fixture.Map.SetTile(2, 1, Tile.Wall);

        var result = PlayerActions.Move(
            fixture.Player, new Vector2i(1, 0), fixture.Context, fixture.Visibility, 6, fixture.Turns.Scheduler);

        Assert.False(result.AdvancesTime);
        Assert.False(result.Succeeded);
        Assert.Equal(new Vector2(1, 1), fixture.World.Get<Position>(fixture.Player).Value);
    }

    [Fact]
    public void OccupiedTileBlocksMovementAndConsumesTurn()
    {
        var fixture = CreateFixture();
        var occupant = fixture.World.Create();
        fixture.World.Set(occupant, new Position { Value = new Vector2(2, 1) });
        fixture.World.Set(occupant, new Location { MapId = "test" });
        fixture.World.Set(occupant, new Actor());

        var result = PlayerActions.Move(
            fixture.Player, new Vector2i(1, 0), fixture.Context, fixture.Visibility, 6, fixture.Turns.Scheduler);

        Assert.Equal(ActionResult.Turn, result);
        Assert.Equal(new Vector2(1, 1), fixture.World.Get<Position>(fixture.Player).Value);
        Assert.Contains(fixture.Context.Events.History,
            simulationEvent => simulationEvent.Type == "movement.blocked");
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
        world.Set(player, new Position { Value = new Vector2(1, 1) });
        world.Set(player, new Location { MapId = "test" });
        world.Set(player, new Actor());
        world.Set(player, new Health { Current = 10, Max = 10 });

        var visibility = new VisibilityMap(map.Width, map.Height);
        var turns = new TurnProcessor();
        turns.AddActor(player);
        var context = new GameContext
        {
            Map = map,
            MapId = "test",
            Maps = maps,
            Log = new Entropy.Game.UI.MessageLog(),
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

        return new Fixture(map, world, player, visibility, turns, context);
    }

    private sealed record Fixture(
        TileMap Map,
        World World,
        Entity Player,
        VisibilityMap Visibility,
        TurnProcessor Turns,
        GameContext Context);
}

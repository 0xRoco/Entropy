using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components.Vitals;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using OpenTK.Mathematics;
using Xunit;

namespace Entropy.Game.Tests;

public class SimulationTimeTests
{
    [Fact]
    public void AdvanceProcessesEveryElapsedMinute()
    {
        var context = CreateContext();

        var advanced = SimulationTime.Advance(context, 5);

        Assert.Equal(5, advanced);
        Assert.Equal(5, context.Clock.TotalMinutes);
        Assert.Equal(475, context.World.Get<Hunger>(context.Player).Current);
        Assert.Equal(235, context.World.Get<Thirst>(context.Player).Current);
        Assert.Equal(955, context.World.Get<Fatigue>(context.Player).Current);
    }

    [Fact]
    public void AdvanceStopsAfterPlayerDies()
    {
        var context = CreateContext();
        context.World.Set(context.Player, new Health { Current = 1, Max = 1 });
        context.World.Set(context.Player, new Hunger { Current = 0, Max = 1, Starving = true });

        var advanced = SimulationTime.Advance(context, 30);

        Assert.Equal(30, advanced);
        Assert.Equal(30, context.Clock.TotalMinutes);
        Assert.Equal(0, context.World.Get<Health>(context.Player).Current);
    }

    private static GameContext CreateContext()
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
        world.Set(player, new Hunger { Current = 480, Max = 480 });
        world.Set(player, new Thirst { Current = 240, Max = 240 });
        world.Set(player, new Fatigue { Current = 960, Max = 960 });

        var visibility = new VisibilityMap(3, 3);
        return new GameContext
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
            Turns = new TurnProcessor(),
            Visibilities = new Dictionary<string, VisibilityMap> { ["test"] = visibility },
            Visibility = visibility,
            ViewRadius = 6,
            DoorDefinitions = new(),
            DoorStates = new()
        };
    }
}

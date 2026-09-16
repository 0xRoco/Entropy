using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.World;
using Entropy.Simulation;
using Xunit;

namespace Entropy.Game.Tests;

public class CoreSimulationStateTests
{
    [Fact]
    public void CoreStateAdvancesClockWithoutGameContextOrUi()
    {
        var world = new World();
        var player = world.Create();
        var map = new TileMap(1, 1);
        var maps = new MapGraph();
        maps.AddMap("test", map);
        var state = new SimulationState(new SimulationContext
        {
            World = world,
            Maps = maps,
            Definitions = new DefinitionRegistry(),
            Rng = new Rng(1234),
            Player = player,
            Clock = new WorldClock(2001, 3, 12, 7, 30),
            Events = new SimulationEventBus()
        });
        var callbacks = 0;

        Assert.Equal(3, state.Advance(3, _ =>
        {
            callbacks++;
            return true;
        }));
        Assert.Equal(3, callbacks);
        Assert.Equal(3, state.Clock.TotalMinutes);
    }
}

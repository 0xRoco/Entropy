using Entropy.Game;
using Entropy.Game.Systems;
using OpenTK.Mathematics;
using Xunit;

namespace Entropy.Game.Tests;

public class SimulationEventBusTests
{
    [Fact]
    public void PublishedEventsCanBeDrainedAndRemainInHistory()
    {
        var bus = new SimulationEventBus();
        var simulationEvent = new SimEvent("noise", "A crash", "test", new Vector2i(2, 3), 12);

        bus.Publish(simulationEvent);

        Assert.Equal(new[] { simulationEvent }, bus.Drain());
        Assert.Empty(bus.Drain());
        Assert.Equal(new[] { simulationEvent }, bus.History);
    }

    [Fact]
    public void ConsequenceEventsCanCarryStableEntityReferences()
    {
        var bus = new SimulationEventBus();
        var simulationEvent = new SimEvent("theft", "An item was stolen", "test", new Vector2i(1, 1), 4,
            ActorStableId: 12,
            TargetStableId: 24);

        bus.Publish(simulationEvent);

        Assert.Equal(12, bus.Drain()[0].ActorStableId);
        Assert.Equal(24, bus.History[0].TargetStableId);
    }
}

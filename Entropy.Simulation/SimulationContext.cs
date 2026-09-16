using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.World;

namespace Entropy.Simulation;

public sealed class SimulationContext
{
    public required World World { get; init; }
    public required MapGraph Maps { get; init; }
    public required DefinitionRegistry Definitions { get; init; }
    public required Rng Rng { get; init; }
    public required Entity Player { get; init; }
    public required WorldClock Clock { get; init; }
    public required SimulationEventBus Events { get; init; }
    public ActorScheduler Scheduler { get; init; } = new();
}

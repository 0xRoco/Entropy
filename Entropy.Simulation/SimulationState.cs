using Entropy.Engine.ECS;
using Entropy.Engine.Core;
using Entropy.Engine.World;

namespace Entropy.Simulation;

public sealed class SimulationState(SimulationContext context)
{
    public SimulationContext Context { get; } = context;
    public World World => Context.World;
    public MapGraph Maps => Context.Maps;
    public DefinitionRegistry Definitions => Context.Definitions;
    public Rng Rng => Context.Rng;
    public Entity Player => Context.Player;
    public WorldClock Clock => Context.Clock;
    public SimulationEventBus Events => Context.Events;
    public ActorScheduler Scheduler => Context.Scheduler;

    public int Advance(int minutes, Func<SimulationState, bool>? processMinute = null)
    {
        if (minutes <= 0)
            return 0;

        var advanced = 0;
        for (var minute = 0; minute < minutes; minute++)
        {
            Clock.Advance(1);
            advanced++;
            if (processMinute is not null && !processMinute(this))
                break;
        }

        return advanced;
    }
}

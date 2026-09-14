using Entropy.Game.Components.Vitals;

namespace Entropy.Game.Systems;

public static class SimulationTime
{
    public static int Advance(GameContext context, int minutes)
    {
        if (minutes <= 0)
            return 0;

        var advanced = 0;
        for (var minute = 0; minute < minutes; minute++)
        {
            context.Clock.Advance(1);
            NeedsSystem.Update(context);
            context.Turns.RunAITurns(context.Player, context);
            advanced++;

            if (!context.World.IsAlive(context.Player) ||
                (context.World.Has<Health>(context.Player) &&
                 context.World.Get<Health>(context.Player).Current <= 0))
            {
                break;
            }
        }

        return advanced;
    }
}

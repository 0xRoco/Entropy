namespace Entropy.Game.Systems;

public static class ActionScheduler
{
    public static bool Process(ActionResult action, Action<int> advance)
    {
        if (!action.AdvancesTime)
            return false;

        advance(action.TimeCostMinutes);
        return true;
    }
}

namespace Entropy.Simulation;

public readonly record struct ActionResult(bool Succeeded, bool ConsumesTurn, int TimeCostMinutes = 1)
{
    public bool AdvancesTime => Succeeded && ConsumesTurn;

    public static ActionResult Failed => new(false, false, 0);
    public static ActionResult Turn => new(true, true);
    public static ActionResult Free => new(true, false, 0);
}

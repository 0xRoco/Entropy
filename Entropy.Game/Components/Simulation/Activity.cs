using Entropy.Engine.ECS;

namespace Entropy.Game.Components.Simulation;

public enum ActivityKind
{
    Sleep,
    Search
}

public struct Activity
{
    public ActivityKind Kind;
    public int RemainingMinutes;
    public int TotalMinutes;
    public StableEntityReference? Target;
}

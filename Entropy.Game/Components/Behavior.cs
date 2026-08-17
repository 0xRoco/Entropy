using Entropy.Game.Behaviors;

namespace Entropy.Game.Components;

public struct Behavior
{
    public required IBehavior Impl { get; init; }
}
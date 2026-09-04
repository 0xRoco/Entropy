namespace Entropy.Game.Behaviors;

public static class BehaviorCatalog
{
    private static readonly Dictionary<string, IBehavior> Behaviors = new()
    {
        ["wander"] = new WanderBehavior(),
        ["schedule"] = new ScheduleBehavior(),
        ["zombie"] = new ZombieBehavior()
    };

    public static IBehavior Get(string id) =>
        Behaviors.TryGetValue(id, out var behavior)
            ? behavior
            : throw new KeyNotFoundException($"Unknown behavior id '{id}'.");
}
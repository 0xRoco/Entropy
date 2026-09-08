using Entropy.Engine.ECS;

namespace Entropy.Game.Behaviors;

public static class BehaviorCatalog
{
    private static readonly Dictionary<string, IBehavior> Behaviors = new()
    {
        ["none"] = new NullBehavior(),
        ["wander"] = new WanderBehavior(),
        ["schedule"] = new ScheduleBehavior(),
        ["zombie"] = new ZombieBehavior()
    };

    public static IBehavior Get(string id) =>
        Behaviors.TryGetValue(id, out var behavior)
            ? behavior
            : throw new KeyNotFoundException($"Unknown behavior id '{id}'.");

    private sealed class NullBehavior : IBehavior
    {
        public void Act(World world, Entity self, GameContext context) { }
    }
}
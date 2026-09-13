using Entropy.Engine.ECS;

namespace Entropy.Game.Behaviors;

public static class BehaviorCatalog
{
    private static readonly Dictionary<string, Func<IBehavior>> Behaviors = new()
    {
        ["none"] = () => new NullBehavior(),
        ["wander"] = () => new WanderBehavior(),
        ["schedule"] = () => new ScheduleBehavior(),
        ["zombie"] = () => new ZombieBehavior(),
        ["respond"] = () => new RespondBehavior()
    };

    public static IBehavior Get(string id) =>
        Behaviors.TryGetValue(id, out var behavior)
            ? behavior()
            : throw new KeyNotFoundException($"Unknown behavior id '{id}'.");

    private sealed class NullBehavior : IBehavior
    {
        public AiIntent Decide(World world, Entity self, GameContext context) => new(AiIntentType.None);
    }
}

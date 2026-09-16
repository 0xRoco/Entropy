using Entropy.Engine.ECS;
using Entropy.Game.Systems;

namespace Entropy.Game.Behaviors;

public class WanderBehavior : IBehavior
{
    public AiIntent Decide(World world, Entity self, AiContext context) =>
        new(AiIntentType.Wander, Chance: 0.3f);
}

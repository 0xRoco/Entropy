using Entropy.Engine.ECS;
using Entropy.Game.Systems;

namespace Entropy.Game.Behaviors;

public class WanderBehavior : IBehavior
{
    public void Act(World world, Entity self, GameContext context)
    {
        AiUtil.Wander(world, self, context.Map, context.Rng, 0.3f);
    }
}
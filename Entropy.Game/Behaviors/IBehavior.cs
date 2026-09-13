using Entropy.Engine.ECS;

namespace Entropy.Game.Behaviors;

public interface IBehavior
{
    AiIntent Decide(World world, Entity self, GameContext context);
}

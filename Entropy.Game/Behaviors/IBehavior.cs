using Entropy.Engine.ECS;

namespace Entropy.Game.Behaviors;

public interface IBehavior
{
    void Act(World world, Entity self, GameContext context);
}
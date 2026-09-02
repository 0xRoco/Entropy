using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components;
using Entropy.Game.Systems;
using OpenTK.Mathematics;

namespace Entropy.Game.Behaviors;

public class RespondBehavior : IBehavior
{
    private readonly Entity _target;

    public RespondBehavior(Entity target) => _target = target;

    public void Act(World world, Entity self, GameContext context)
    {
        if (!world.IsAlive(_target) || !world.Has<Wanted>(_target))
        {
            context.Log.Add("The officer shrugs and leaves.", Color4.LightGray);
            world.Destroy(self);
            return;
        }

        ref var myPos = ref world.Get<Position>(self).Value;
        var targetPos = world.Get<Position>(_target).Value;

        if (AiUtil.IsAdjacent(myPos, targetPos))
        {
            var holding = ConsequenceSystem.FindHoldingTile(context.Map);
            world.Get<Position>(_target).Value = new Vector2(holding.X, holding.Y);
            Fov.Compute(holding, context.ViewRadius, context.Map, context.Visibility);
            world.Remove<Wanted>(_target);
            context.Log.Add("The officer grabs you. \"You're under arrest.\"", Color4.Red);
            context.Log.Add("You are held at the station. (Prison later)", Color4.LightGray);
            world.Destroy(self);
            return;
        }

        AiUtil.StepToward(world, self, context.Map,
            new Vector2i((int)targetPos.X, (int)targetPos.Y));
    }
}
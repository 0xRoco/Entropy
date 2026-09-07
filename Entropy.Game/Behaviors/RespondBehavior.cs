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

        var targetMapId = world.Get<Location>(_target).MapId;
        var targetPosition = world.Get<Position>(_target).Value;

        var selfMapId = world.Get<Location>(self).MapId;
        var selfPosition = world.Get<Position>(self).Value;

        if (selfMapId == targetMapId &&
            AiUtil.IsAdjacent(selfPosition, targetPosition))
        {
            var holding = ConsequenceSystem.FindHoldingTile(context.Maps[targetMapId]);

            world.Set(_target, new Location { MapId = targetMapId });
            world.Get<Position>(_target).Value =
                new Vector2(holding.X, holding.Y);

            context.MapId = targetMapId;
            context.Map = context.Maps[targetMapId];
            context.Visibility = context.Visibilities[targetMapId];

            Fov.Compute(
                holding,
                context.ViewRadius,
                context.Map,
                context.Visibility);

            world.Remove<Wanted>(_target);

            context.Log.Add("The officer grabs you. \"You're under arrest.\"", Color4.Red);
            context.Log.Add("You are held at the station. (Prison arrives later.)", Color4.LightGray);

            world.Destroy(self);
            return;
        }

        AiUtil.TravelToward(
            world,
            self,
            context,
            targetMapId,
            new Vector2i((int)targetPosition.X, (int)targetPosition.Y));
    }
}
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components;
using Entropy.Game.Components.AI;
using Entropy.Game.Components.Tags;
using Entropy.Game.Systems;
using OpenTK.Mathematics;

namespace Entropy.Game.Behaviors;

public class RespondBehavior : IBehavior
{
    public AiIntent Decide(World world, Entity self, GameContext context)
    {
        if (!world.Has<AIState>(self)) return new AiIntent(AiIntentType.None);
        var target = world.Get<AIState>(self).Target;
        if (target is not { } _target) return new AiIntent(AiIntentType.Despawn);

        if (!world.IsAlive(_target) || !world.Has<Wanted>(_target))
            return new AiIntent(AiIntentType.Despawn);

        var targetMapId = world.Get<Location>(_target).MapId;
        var targetPosition = world.Get<Position>(_target).Value;

        var selfMapId = world.Get<Location>(self).MapId;
        var selfPosition = world.Get<Position>(self).Value;

        if (selfMapId == targetMapId &&
            AiUtil.IsAdjacent(selfPosition, targetPosition))
            return new AiIntent(AiIntentType.Arrest, Target: _target);

        return new AiIntent(
            AiIntentType.TravelToward,
            MapId: targetMapId,
            Destination: new Vector2i((int)targetPosition.X, (int)targetPosition.Y));
    }

    public static void ExecuteArrest(World world, Entity officer, Entity target, GameContext context)
    {
        var targetMapId = world.Get<Location>(target).MapId;
        var holding = ConsequenceSystem.FindHoldingTile(context.Maps[targetMapId]);
        world.Set(target, new Location { MapId = targetMapId });
        world.Get<Position>(target).Value = new Vector2(holding.X, holding.Y);
        context.MapId = targetMapId;
        context.Map = context.Maps[targetMapId];
        context.Visibility = context.Visibilities[targetMapId];
        Fov.Compute(holding, context.ViewRadius, context.Map, context.Visibility);
        world.Remove<Wanted>(target);
        context.Log.Add("The officer grabs you. \"You're under arrest.\"", Color4.Red);
        context.Log.Add("You are held at the station. (Prison arrives later.)", Color4.LightGray);
        world.Destroy(officer);
    }
}

using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components;
using Entropy.Game.Components.AI;
using Entropy.Game.Systems;
using OpenTK.Mathematics;

namespace Entropy.Game.Behaviors;

public class ScheduleBehavior : IBehavior
{
    private const int WanderRadius = 6;

    public AiIntent Decide(World world, Entity self, AiContext context)
    {
        if (!world.Has<Schedule>(self) || !world.Has<Home>(self))
            return new AiIntent(AiIntentType.None);

        var minuteOfDay = context.Clock.MinuteOfDay;
        var activity = world.Get<Schedule>(self)
            .ActivityAt(minuteOfDay, context.Clock.Weekday);

        var home = world.Get<Home>(self);

        switch (activity)
        {
            case ScheduleActivity.Sleep:
            case ScheduleActivity.Home:
                return new AiIntent(AiIntentType.TravelToward, MapId: home.MapId, Destination: home.Tile);

            case ScheduleActivity.Work:
                if (world.Has<Workplace>(self))
                {
                    var work = world.Get<Workplace>(self);
                    return new AiIntent(AiIntentType.TravelToward, MapId: work.MapId, Destination: work.Tile);
                }
                return new AiIntent(AiIntentType.WanderNear, Anchor: home.Tile, Radius: WanderRadius, Chance: 0.3f);

            default:
                return new AiIntent(AiIntentType.WanderNear, Anchor: home.Tile, Radius: WanderRadius, Chance: 0.3f);
        }
    }

    private static void GoTo(
        World world,
        Entity self,
        AiContext context,
        string destinationMapId,
        Vector2i anchor)
    {
        var currentMapId = world.Get<Location>(self).MapId;

        if (currentMapId == destinationMapId)
        {
            var position = world.Get<Position>(self).Value;
            
            if (AiUtil.Manhattan(position, new Vector2(anchor.X, anchor.Y)) <= 1)
                return;
        }

        AiUtil.TravelToward(world, self, context.Maps, destinationMapId, anchor);
    }

    private static void WanderAtHome(
        World world,
        Entity self,
        AiContext context,
        Home home)
    {
        var currentMapId = world.Get<Location>(self).MapId;

        if (currentMapId != home.MapId)
        {
        AiUtil.TravelToward(world, self, context.Maps, home.MapId, home.Tile);
            return;
        }

        AiUtil.WanderNear(
            world,
            self,
            context.Maps[home.MapId],
            context.Rng,
            home.Tile,
            WanderRadius,
            0.3f);
    }
}

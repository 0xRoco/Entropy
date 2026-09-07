using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components;
using Entropy.Game.Systems;
using OpenTK.Mathematics;

namespace Entropy.Game.Behaviors;

public class ScheduleBehavior : IBehavior
{
    private const int WanderRadius = 6;

    public void Act(World world, Entity self, GameContext context)
    {
        if (!world.Has<Schedule>(self) || !world.Has<Home>(self))
            return;

        var minuteOfDay = context.Clock.MinuteOfDay;
        var activity = world.Get<Schedule>(self)
            .ActivityAt(minuteOfDay, context.Clock.Weekday);

        var home = world.Get<Home>(self);

        switch (activity)
        {
            case ScheduleActivity.Sleep:
            case ScheduleActivity.Home:
                GoTo(world, self, context, home.MapId, home.Tile);
                break;

            case ScheduleActivity.Work:
                if (world.Has<Workplace>(self))
                {
                    var work = world.Get<Workplace>(self);
                    GoTo(world, self, context, work.MapId, work.Tile);
                }
                else
                {
                    WanderAtHome(world, self, context, home);
                }
                break;

            default:
                WanderAtHome(world, self, context, home);
                break;
        }
    }

    private static void GoTo(
        World world,
        Entity self,
        GameContext context,
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

        AiUtil.TravelToward(world, self, context, destinationMapId, anchor);
    }

    private static void WanderAtHome(
        World world,
        Entity self,
        GameContext context,
        Home home)
    {
        var currentMapId = world.Get<Location>(self).MapId;

        if (currentMapId != home.MapId)
        {
            AiUtil.TravelToward(world, self, context, home.MapId, home.Tile);
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
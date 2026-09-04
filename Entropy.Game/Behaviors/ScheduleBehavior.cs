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
        if (!world.Has<Schedule>(self)) return;

        var minuteOfDay = context.Clock.TotalMinutes % 1440;
        var activity = world.Get<Schedule>(self).ActivityAt(minuteOfDay, context.Clock.Weekday);

        switch (activity)
        {
            case ScheduleActivity.Sleep:
            case ScheduleActivity.Home:
                GoTo(world, self, context, world.Get<Home>(self).Tile);
                break;

            case ScheduleActivity.Work:
                if (world.Has<Workplace>(self))
                    GoTo(world, self, context, world.Get<Workplace>(self).Tile);
                else
                    AiUtil.WanderNear(world, self, context.Map, context.Rng,
                        world.Get<Home>(self).Tile, WanderRadius, 0.3f);
                break;

            default:
                AiUtil.WanderNear(world, self, context.Map, context.Rng,
                    world.Get<Home>(self).Tile, WanderRadius, 0.3f);
                break;
        }
    }

    private static void GoTo(World world, Entity self, GameContext context, Vector2i anchor)
    {
        ref var pos = ref world.Get<Position>(self);
        var here = AiUtil.ToTile(pos.Value);

        if (AiUtil.Manhattan(pos.Value, new Vector2(anchor.X, anchor.Y)) <= 1) return;

        AiUtil.StepToward(world, self, context.Map, anchor);
    }
}
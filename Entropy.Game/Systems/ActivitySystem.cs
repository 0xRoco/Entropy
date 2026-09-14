using Entropy.Engine.ECS;
using Entropy.Game.Components.Simulation;
using Entropy.Game.Components.Inventory;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public enum ActivityState
{
    InProgress,
    Completed,
    Cancelled,
    Interrupted,
    NotActive
}

public readonly record struct ActivityTick(ActivityState State, int RemainingMinutes)
{
    public bool IsActive => State == ActivityState.InProgress;
}

public static class ActivitySystem
{
    public static bool IsActive(World world, Entity actor) =>
        world.IsAlive(actor) && world.Has<Activity>(actor);

    public static bool Start(GameContext context, Entity actor, Activity activity)
    {
        if (!context.World.IsAlive(actor) || context.World.Has<Activity>(actor))
            return false;
        if (activity.RemainingMinutes <= 0 || activity.TotalMinutes < activity.RemainingMinutes)
            return false;

        context.World.Set(actor, activity);
        return true;
    }

    public static ActivityTick Advance(GameContext context, Entity actor, int minutes = 1)
    {
        if (!IsActive(context.World, actor))
            return new ActivityTick(ActivityState.NotActive, 0);
        if (minutes <= 0)
            return new ActivityTick(ActivityState.InProgress,
                context.World.Get<Activity>(actor).RemainingMinutes);

        ref var activity = ref context.World.Get<Activity>(actor);
        switch (activity.Kind)
        {
            case ActivityKind.Sleep:
                SleepSystem.Recover(context, actor);
                break;
            case ActivityKind.Search:
                if (activity.Target is not { } targetReference)
                    return Interrupt(context, actor, "Your search is interrupted.");
                var target = targetReference.Resolve(context.World);
                if (!context.World.IsAlive(target) || !context.World.Has<Container>(target))
                    return Interrupt(context, actor, "Your search is interrupted.");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(activity.Kind), activity.Kind, null);
        }

        activity.RemainingMinutes = Math.Max(0, activity.RemainingMinutes - minutes);
        if (activity.RemainingMinutes == 0)
            return Complete(context, actor);

        if (activity.Kind == ActivityKind.Sleep)
        {
            var reason = SleepSystem.WakeReason(context, actor);
            if (reason is not null)
                return Interrupt(context, actor, reason);
        }

        return new ActivityTick(ActivityState.InProgress, activity.RemainingMinutes);
    }

    public static ActivityTick Complete(GameContext context, Entity actor)
    {
        if (!IsActive(context.World, actor))
            return new ActivityTick(ActivityState.NotActive, 0);

        var activity = context.World.Get<Activity>(actor);
        context.World.Remove<Activity>(actor);
        if (activity.Kind == ActivityKind.Sleep)
            context.Log.Add("You wake up feeling rested.", Color4.LightGray);
        if (activity.Kind == ActivityKind.Search && activity.Target is { } targetReference)
        {
            var target = targetReference.Resolve(context.World);
            if (context.World.IsAlive(target))
            SearchSystem.Complete(context, actor, target);
        }
        return new ActivityTick(ActivityState.Completed, 0);
    }

    public static ActivityTick Cancel(GameContext context, Entity actor, string reason)
    {
        if (!IsActive(context.World, actor))
            return new ActivityTick(ActivityState.NotActive, 0);

        context.World.Remove<Activity>(actor);
        if (!string.IsNullOrWhiteSpace(reason))
            context.Log.Add(reason, Color4.LightGray);
        return new ActivityTick(ActivityState.Cancelled, 0);
    }

    public static ActivityTick Interrupt(GameContext context, Entity actor, string reason)
    {
        if (!IsActive(context.World, actor))
            return new ActivityTick(ActivityState.NotActive, 0);

        context.World.Remove<Activity>(actor);
        if (!string.IsNullOrWhiteSpace(reason))
            context.Log.Add(reason, Color4.LightGray);
        return new ActivityTick(ActivityState.Interrupted, 0);
    }
}

using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components;
using Entropy.Game.Components.Simulation;
using Entropy.Game.Components.Tags;
using Entropy.Game.Components.Vitals;
using OpenTK.Mathematics;
using Entropy.Simulation;

namespace Entropy.Game.Systems;

public static class SleepSystem
{
    public const int RecoveryPerTurn = 3;

    public static ActionResult FallAsleep(IGameRuntimeContext context, Entity player, Entity bed)
    {
        if (!context.World.IsAlive(bed) || ActivitySystem.IsActive(context.World, player))
            return ActionResult.Failed;

        ActivitySystem.Start(context, player, new Activity
        {
            Kind = ActivityKind.Sleep,
            RemainingMinutes = 480,
            TotalMinutes = 480,
            Target = StableEntityReference.From(context.World, bed)
        });
        context.Events.Publish(new SimEvent(
            "sleep.started",
            "You lie down and fall asleep.",
            context.World.Get<Location>(player).MapId,
            new Vector2i((int)context.World.Get<Position>(player).Value.X, (int)context.World.Get<Position>(player).Value.Y),
            context.Clock.MinuteOfDay,
            context.World.StableId(player),
            context.World.IsAlive(bed) ? context.World.StableId(bed) : null));
        return ActionResult.Turn;
    }

    public static void Wake(IGameRuntimeContext context, Entity player, string reason)
    {
        ActivitySystem.Interrupt(context, player, reason);
    }

    public static void Recover(IGameRuntimeContext context, Entity player)
    {
        ref var fatigue = ref context.World.Get<Fatigue>(player);
        fatigue.Current = Math.Min(fatigue.Max, fatigue.Current + RecoveryPerTurn);
    }

    public static string? WakeReason(IGameRuntimeContext context, Entity player)
    {
        var world = context.World;

        var fatigue = world.Get<Fatigue>(player);
        if (fatigue.Current >= fatigue.Max)
            return "You wake up feeling rested.";

        if (world.Get<Hunger>(player).Current <= 0)
            return "Hunger wakes you up.";

        if (world.Get<Thirst>(player).Current <= 0)
            return "Thirst wakes you up.";

        if (HostileNearby(context, player))
            return "You are woken by a disturbance!";

        return null;
    }

    private static bool HostileNearby(IGameRuntimeContext context, Entity player)
    {
        var world = context.World;
        if (!world.Has<Location>(player))
            return false;

        var mapId = world.Get<Location>(player).MapId;
        var position = world.Get<Position>(player).Value;

        foreach (var entity in world.Query<Position, Hostile>())
        {
            if (!world.Has<Location>(entity) ||
                world.Get<Location>(entity).MapId != mapId)
                continue;

            var hostilePos = world.Get<Position>(entity).Value;
            var x = (int)hostilePos.X;
            var y = (int)hostilePos.Y;

            if (Math.Abs(x - (int)position.X) > ViewRadius ||
                Math.Abs(y - (int)position.Y) > ViewRadius)
                continue;

            if (context.Visibility.IsVisible(x, y))
                return true;
        }

        return false;
    }

    private const int ViewRadius = 6;
}

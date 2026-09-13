using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components;
using Entropy.Game.Components.Tags;
using Entropy.Game.Components.Vitals;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public static class SleepSystem
{
    public const int RecoveryPerTurn = 3;

    public static void FallAsleep(GameContext context, Entity player, Entity bed)
    {
        context.World.Set(player, new Sleeping { Bed = bed });
        context.Log.Add("You lie down and fall asleep.");
    }

    public static void Wake(GameContext context, Entity player, string reason)
    {
        context.World.Remove<Sleeping>(player);
        context.Log.Add(reason, Color4.LightGray);
    }

    public static void Recover(GameContext context, Entity player)
    {
        ref var fatigue = ref context.World.Get<Fatigue>(player);
        fatigue.Current = Math.Min(fatigue.Max, fatigue.Current + RecoveryPerTurn);
    }

    public static string? WakeReason(GameContext context, Entity player)
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

    private static bool HostileNearby(GameContext context, Entity player)
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

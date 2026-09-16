using Entropy.Engine.Core;
using Entropy.Engine.UI;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components;
using Entropy.Game.Components.Vitals;
using Entropy.Game.Components.Spatial;
using Entropy.Simulation;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public static class NeedsSystem
{
    public static void Update(World world, WorldClock clock, Action<string, Color4?> notify)
    {
        var minute = clock.TotalMinutes;

        foreach (var entity in world.Query<Hunger>())
        {
            ref var hunger = ref world.Get<Hunger>(entity);
            if (hunger.Current > 0)
            {
                hunger.Current--;
                if (hunger.Current == 0 && !hunger.Starving)
                {
                    hunger.Starving = true;
                    notify("You are starving!", UiTheme.Danger);
                }
            }
            else if (minute % 30 == 0)
                Damage(world, entity, 1);
        }

        foreach (var entity in world.Query<Thirst>())
        {
            ref var thirst = ref world.Get<Thirst>(entity);
            if (thirst.Current > 0)
            {
                thirst.Current--;
                if (thirst.Current == 0 && !thirst.Parched)
                {
                    thirst.Parched = true;
                    notify("You are parched!", UiTheme.Danger);
                }
            }
            else if (minute % 15 == 0)
                Damage(world, entity, 1);
        }

        foreach (var entity in world.Query<Fatigue>())
        {
            ref var fatigue = ref world.Get<Fatigue>(entity);
            if (fatigue.Current > 0) fatigue.Current--;
        }
    }

    public static void Update(IGameRuntimeContext context) =>
        Update(context.World, context.Clock, (message, color) =>
        {
            var player = context.Player;
            if (!context.World.IsAlive(player) || !context.World.Has<Position>(player) || !context.World.Has<Location>(player))
                return;
            var position = context.World.Get<Position>(player).Value;
            context.Events.Publish(new SimEvent(
                "needs.warning",
                message,
                context.World.Get<Location>(player).MapId,
                new OpenTK.Mathematics.Vector2i((int)position.X, (int)position.Y),
                context.Clock.MinuteOfDay,
                context.World.StableId(player)));
        });

    private static void Damage(Entropy.Engine.ECS.World world, Entropy.Engine.ECS.Entity entity, int amount)
    {
        if (!world.Has<Health>(entity)) return;
        ref var hp = ref world.Get<Health>(entity);
        hp.Current -= amount;
    }
}

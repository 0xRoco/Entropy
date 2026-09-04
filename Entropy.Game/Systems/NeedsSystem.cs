using Entropy.Engine.UI;
using Entropy.Game.Components;

namespace Entropy.Game.Systems;

public static class NeedsSystem
{
    public static void Update(GameContext ctx)
    {
        var world = ctx.World;
        var minute = ctx.Clock.TotalMinutes;

        foreach (var entity in world.Query<Hunger>())
        {
            ref var hunger = ref world.Get<Hunger>(entity);
            if (hunger.Current > 0)
            {
                hunger.Current--;
                if (hunger.Current == 0 && !hunger.Starving)
                {
                    hunger.Starving = true;
                    ctx.Log.Add("You are starving!", UiTheme.Danger);
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
                    ctx.Log.Add("You are parched!", UiTheme.Danger);
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

    private static void Damage(Entropy.Engine.ECS.World world, Entropy.Engine.ECS.Entity entity, int amount)
    {
        if (!world.Has<Health>(entity)) return;
        ref var hp = ref world.Get<Health>(entity);
        hp.Current -= amount;
    }
}
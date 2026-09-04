using Entropy.Engine.ECS;
using Entropy.Engine.UI;
using Entropy.Game.Components;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public static class ItemUse
{
    public static bool Use(World world, GameContext context, Entity item, Entity user)
    {
        var name = world.Get<ItemIdentity>(item).Name;
        var used = false;

        if (world.Has<Healing>(item))
        {
            ref var hp = ref world.Get<Health>(user);
            if (hp.Current < hp.Max)
            {
                ref var healing = ref world.Get<Healing>(item);
                hp.Current = Math.Min(hp.Current + healing.Amount, hp.Max);
                context.Log.Add($"You use the {name} and recover {healing.Amount} health.", UiTheme.Valid);
                used = true;
            }
            else
                context.Log.Add("You are already at full health.", UiTheme.TextDim);
        }

        if (world.Has<Nutrition>(item))
        {
            ref var hunger = ref world.Get<Hunger>(user);
            var amount = world.Get<Nutrition>(item).Amount;
            hunger.Current = Math.Min(hunger.Current + amount, hunger.Max);
            hunger.Starving = false;
            context.Log.Add($"You eat the {name}. (+{amount})", UiTheme.Valid);
            used = true;
        }

        if (world.Has<Hydration>(item))
        {
            ref var thirst = ref world.Get<Thirst>(user);
            var amount = world.Get<Hydration>(item).Amount;
            thirst.Current = Math.Min(thirst.Current + amount, thirst.Max);
            thirst.Parched = false;
            context.Log.Add($"You drink the {name}. (+{amount})", UiTheme.Valid);
            used = true;
        }

        if (!used)
        {
            context.Log.Add($"You can't find a way to use the {name}.", UiTheme.TextDim);
            return false;
        }

        if (world.Has<Stackable>(item))
        {
            ref var stack = ref world.Get<Stackable>(item);
            if (--stack.Count > 0) return true;
        }
        ItemSystem.RemoveFromContainer(world, item);
        world.Destroy(item);
        return true;
    }
}
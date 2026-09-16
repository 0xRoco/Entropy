using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.UI;
using Entropy.Game.Components;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.ItemEffects;
using Entropy.Game.Components.Vitals;
using OpenTK.Mathematics;
using Entropy.Simulation;

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
                Publish(context, user, "item.used", $"You use the {name} and recover {healing.Amount} health.", item);
                used = true;
            }
            else
                Publish(context, user, "item.use.failed", "You are already at full health.", item);
        }

        if (world.Has<Nutrition>(item))
        {
            ref var hunger = ref world.Get<Hunger>(user);
            var amount = world.Get<Nutrition>(item).Amount;
            hunger.Current = Math.Min(hunger.Current + amount, hunger.Max);
            hunger.Starving = false;
            Publish(context, user, "item.used", $"You eat the {name}. (+{amount})", item);
            used = true;
        }

        if (world.Has<Hydration>(item))
        {
            ref var thirst = ref world.Get<Thirst>(user);
            var amount = world.Get<Hydration>(item).Amount;
            thirst.Current = Math.Min(thirst.Current + amount, thirst.Max);
            thirst.Parched = false;
            Publish(context, user, "item.used", $"You drink the {name}. (+{amount})", item);
            used = true;
        }

        if (!used)
        {
            Publish(context, user, "item.use.failed", $"You can't find a way to use the {name}.", item);
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

    private static void Publish(GameContext context, Entity user, string type, string description, Entity item)
    {
        var position = context.World.Get<Position>(user).Value;
        context.Events.Publish(new SimEvent(
            type,
            description,
            context.World.Get<Location>(user).MapId,
            new Vector2i((int)position.X, (int)position.Y),
            context.Clock.MinuteOfDay,
            context.World.StableId(user),
            context.World.IsAlive(item) ? context.World.StableId(item) : null));
    }
}

using Entropy.Engine.ECS;
using Entropy.Game.Components;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public static class ItemUse
{
    public static bool Use(World world, GameContext context, Entity item, Entity user)
    {
        if (!world.Has<Healing>(item))
        {
            context.Log.Add($"You can't find a way to use the {world.Get<ItemIdentity>(item).Name}.", Color4.LightGray);
            return false;
        }
        
        ref var healing = ref world.Get<Healing>(item);
        ref var hp = ref world.Get<Health>(user);
        if (hp.Current >= hp.Max)
        {
            context.Log.Add("You are already at full health.", Color4.LightGray);
            return false;
        }
        
        hp.Current = Math.Min(hp.Current + healing.Amount, hp.Max);
        context.Log.Add($"You use the {world.Get<ItemIdentity>(item).Name} and recover {healing.Amount} health.", Color4.LightGreen);

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
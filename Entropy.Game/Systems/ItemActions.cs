using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public record ItemAction(string Label, char Hotkey, Action<GameContext, Entity, Entity> Execute);

public static class ItemActions
{
    public static List<ItemAction> AvailableFor(GameContext context, Entity item)
    {
        var actions = new List<ItemAction>();
        var world = context.World;
        
        if (world.Has<Damage>(item))
            actions.Add(new ItemAction("Wield", 'w', Wield));
        
        if (world.Has<Healing>(item))
            actions.Add(new ItemAction("Use", 'u', Use));
        
        actions.Add(new ItemAction("Drop", 'd', Drop));
        actions.Add(new ItemAction("Examine", 'e', Examine));
        
        return actions;
    }

    private static void Wield(GameContext context, Entity player, Entity item)
    {
        var world = context.World;
        var log = context.Log;
        var items = ItemSystem.GetItems(world, player);
        var index = items.IndexOf(item);
        
        if (index < 0 || index >= items.Count) return;
        
        if (!world.Has<Damage>(item))
        {
            log.Add($"You can't wield the {world.Get<ItemIdentity>(item).Name}.", Color4.LightGray);
            return;
        }

        world.Set(player, new Equipped { Item = item });
        log.Add($"You wield the {world.Get<ItemIdentity>(item).Name}.", Color4.Cyan);
    }

    private static void Drop(GameContext context, Entity player, Entity item)
    {
        var pos = context.World.Get<Position>(player).Value;
        var name = context.World.Get<ItemIdentity>(item).Name;

        if (context.World.Has<Equipped>(player) && context.World.Get<Equipped>(player).Item.Equals(item))
            context.World.Remove<Equipped>(player);

        ItemSystem.Drop(context.World, item, (int)pos.X, (int)pos.Y);
        context.Log.Add($"You drop the {name}.");
    }

    private static void Use(GameContext context, Entity player, Entity item)
    {
        ItemUse.Use(context.World, context, item, player);
    }

    private static void Examine(GameContext context, Entity player, Entity item)
    {
        var identity = context.World.Get<ItemIdentity>(item);
        var def = context.Definitions.Item(identity.DefinitionId);
        context.Log.Add($"{identity.Name}: {def.Description ?? "No Description"}", Color4.LightGray);
    }
}
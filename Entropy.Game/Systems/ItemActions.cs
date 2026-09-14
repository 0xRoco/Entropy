using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.ItemEffects;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public record ItemAction(string Label, char Hotkey, Func<GameContext, Entity, Entity, ActionResult> Execute);

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
        else if (world.Has<Nutrition>(item))
            actions.Add(new ItemAction("Eat", 'u', Use));
        else if (world.Has<Hydration>(item))
            actions.Add(new ItemAction("Drink", 'u', Use));

        actions.Add(new ItemAction("Drop", 'd', Drop));
        actions.Add(new ItemAction("Examine", 'e', Examine));

        return actions;
    }

    private static ActionResult Wield(GameContext context, Entity player, Entity item)
    {
        var world = context.World;
        var log = context.Log;
        var items = ItemSystem.GetItems(world, player);
        var index = items.IndexOf(item);

        if (index < 0 || index >= items.Count) return ActionResult.Failed;

        if (!world.Has<Damage>(item))
        {
            log.Add($"You can't wield the {world.Get<ItemIdentity>(item).Name}.", Color4.LightGray);
            return ActionResult.Failed;
        }

        world.Set(player, new Equipped { Item = StableEntityReference.From(world, item) });
        log.Add($"You wield the {world.Get<ItemIdentity>(item).Name}.", Color4.Cyan);
        return ActionResult.Free;
    }

    private static ActionResult Drop(GameContext context, Entity player, Entity item)
    {
        if (!context.World.IsAlive(item)) return ActionResult.Failed;
        var pos = context.World.Get<Position>(player).Value;
        var mapId = context.World.Get<Location>(player).MapId;
        var name = context.World.Get<ItemIdentity>(item).Name;

        if (context.World.Has<Equipped>(player) &&
            context.World.Get<Equipped>(player).Item.Resolve(context.World).Equals(item))
            context.World.Remove<Equipped>(player);

        ItemSystem.Drop(context.World, mapId, item, (int)pos.X, (int)pos.Y);
        context.Log.Add($"You drop the {name}.");
        return ActionResult.Turn;
    }

    private static ActionResult Use(GameContext context, Entity player, Entity item)
    {
        return ItemUse.Use(context.World, context, item, player)
            ? ActionResult.Turn
            : ActionResult.Failed;
    }

    private static ActionResult Examine(GameContext context, Entity player, Entity item)
    {
        if (!context.World.IsAlive(item)) return ActionResult.Failed;
        var identity = context.World.Get<ItemIdentity>(item);
        var def = context.Definitions.Item(identity.DefinitionId);
        context.Log.Add($"{identity.Name}: {def.Description ?? "No Description"}", Color4.LightGray);
        return ActionResult.Free;
    }
}

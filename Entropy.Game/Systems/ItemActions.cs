using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.ItemEffects;
using Entropy.Simulation;
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
        var items = ItemSystem.GetItems(world, player);
        var index = items.IndexOf(item);

        if (index < 0 || index >= items.Count) return ActionResult.Failed;

        if (!world.Has<Damage>(item))
        {
            Publish(context, player, "item.use.failed", $"You can't wield the {world.Get<ItemIdentity>(item).Name}.", item);
            return ActionResult.Failed;
        }

        world.Set(player, new Equipped { Item = StableEntityReference.From(world, item) });
        Publish(context, player, "item.wielded", $"You wield the {world.Get<ItemIdentity>(item).Name}.", item);
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
        context.Events.Publish(new Entropy.Simulation.SimEvent(
            "item.dropped",
            $"You drop the {name}.",
            mapId,
            new Vector2i((int)pos.X, (int)pos.Y),
            context.Clock.MinuteOfDay,
            context.World.StableId(player),
            context.World.StableId(item)));
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
        Publish(context, player, "item.examined", $"{identity.Name}: {def.Description ?? "No Description"}", item);
        return ActionResult.Free;
    }

    private static void Publish(GameContext context, Entity actor, string type, string description, Entity target)
    {
        var position = context.World.Get<Position>(actor).Value;
        context.Events.Publish(new SimEvent(
            type,
            description,
            context.World.Get<Location>(actor).MapId,
            new Vector2i((int)position.X, (int)position.Y),
            context.Clock.MinuteOfDay,
            context.World.StableId(actor),
            context.World.IsAlive(target) ? context.World.StableId(target) : null));
    }
}

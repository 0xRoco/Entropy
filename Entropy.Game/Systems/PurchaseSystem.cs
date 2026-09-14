using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public static class PurchaseSystem
{
    public static bool IsShopStock(GameContext context, Entity container)
    {
        if (!context.World.Has<WorldObjectIdentity>(container))
            return false;

        var definitionId = context.World.Get<WorldObjectIdentity>(container).DefinitionId;
        return context.Definitions.WorldObject(definitionId).HasFlag("shop_stock");
    }

    public static bool TryPurchase(GameContext context, Entity buyer, Entity item, Entity shop)
    {
        if (!CanTrade(context, buyer, item, shop) || !context.World.Has<Wallet>(buyer))
            return false;

        var identity = context.World.Get<ItemIdentity>(item);
        var definition = context.Definitions.Item(identity.DefinitionId);
        var count = context.World.Has<Stackable>(item) ? context.World.Get<Stackable>(item).Count : 1;
        var cost = (long)definition.PriceCents * count;
        ref var wallet = ref context.World.Get<Wallet>(buyer);
        if (definition.PriceCents <= 0 || cost > int.MaxValue || wallet.CashCents < cost)
        {
            context.Log.Add($"You cannot afford the {identity.Name}.", Color4.Yellow);
            return false;
        }

        if (!ItemSystem.Transfer(context.World, item, buyer))
            return false;

        wallet.CashCents -= (int)cost;
        context.Log.Add($"You buy the {identity.Name} for ${cost / 100}.{cost % 100:00}.");
        return true;
    }

    public static bool TrySteal(GameContext context, Entity thief, Entity item, Entity shop)
    {
        if (!CanTrade(context, thief, item, shop))
            return false;

        var identity = context.World.Get<ItemIdentity>(item);
        if (!ItemSystem.Transfer(context.World, item, thief))
            return false;

        var location = context.World.Has<Position>(shop)
            ? context.World.Get<Position>(shop).Value
            : Vector2.Zero;
        ConsequenceSystem.Report(
            context,
            "theft",
            $"{identity.Name} stolen from a store",
            new Vector2i((int)location.X, (int)location.Y),
            thief,
            shop);
        context.Log.Add($"You steal the {identity.Name}.", Color4.Yellow);
        return true;
    }

    private static bool CanTrade(GameContext context, Entity actor, Entity item, Entity shop)
    {
        return context.World.IsAlive(actor) &&
               context.World.IsAlive(item) &&
               context.World.IsAlive(shop) &&
               context.World.Has<Item>(item) &&
               context.World.Has<InContainer>(item) &&
               context.World.Get<InContainer>(item).Parent.Resolve(context.World).Equals(shop) &&
               IsShopStock(context, shop);
    }
}

using Entropy.Engine.ECS;

namespace Entropy.Game.Systems;

public static class ArrivalSystem
{
    public const int BribeCostCents = 2500;

    public static ActionResult GrantTemporaryPermit(IGameRuntimeContext context, Entity player)
    {
        if (context.Arrival.HasTemporaryPermit)
            return ActionResult.Free;

        context.Arrival.HasTemporaryPermit = true;
        context.Arrival.PermitExpiryMinute = context.Clock.TotalMinutes + 3 * 24 * 60;
        context.Arrival.LegalIdentityStatus = "temporary_entrant";
        context.Events.Publish(new SimEvent(
            "arrival.permit_granted",
            "The guard issues a temporary entry permit. It expires in three days.",
            context.MapId,
            new OpenTK.Mathematics.Vector2i(
                (int)context.World.Get<Entropy.Engine.ECS.Components.Position>(player).Value.X,
                (int)context.World.Get<Entropy.Engine.ECS.Components.Position>(player).Value.Y),
            context.Clock.MinuteOfDay,
            context.World.StableId(player)));
        return ActionResult.Turn;
    }

    public static ActionResult BribeForTemporaryPermit(IGameRuntimeContext context, Entity player)
    {
        if (!context.World.Has<Entropy.Game.Components.Inventory.Wallet>(player) ||
            context.World.Get<Entropy.Game.Components.Inventory.Wallet>(player).CashCents < BribeCostCents)
            return ActionResult.Failed;

        var wallet = context.World.Get<Entropy.Game.Components.Inventory.Wallet>(player);
        wallet.CashCents -= BribeCostCents;
        context.World.Set(player, wallet);
        var result = GrantTemporaryPermit(context, player);
        context.Arrival.LegalIdentityStatus = "temporary_entrant_bribed";
        context.Events.Publish(new SimEvent(
            "arrival.bribed",
            "The guard accepts your cash and waves you through.",
            context.MapId,
            new OpenTK.Mathematics.Vector2i(
                (int)context.World.Get<Entropy.Engine.ECS.Components.Position>(player).Value.X,
                (int)context.World.Get<Entropy.Engine.ECS.Components.Position>(player).Value.Y),
            context.Clock.MinuteOfDay,
            context.World.StableId(player)));
        return result;
    }
}

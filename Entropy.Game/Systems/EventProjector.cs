using Entropy.Game.UI;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public static class EventProjector
{
    public static void Project(SimulationEventBus events, MessageLog log)
    {
        foreach (var simulationEvent in events.Drain())
        {
            var color = simulationEvent.Type switch
            {
                "combat.kill" => Color4.Yellow,
                "combat.message" => Color4.LightGray,
                "door.force" or "container.force" or "noise" => Color4.OrangeRed,
                "door.unlock" or "container.unlock" => Color4.Cyan,
                "noise.response" => Color4.Yellow,
                "crime.witnessed" or "police.response" => Color4.Yellow,
                "police.arrest" => Color4.Red,
                "search.started" or "purchase.completed" => Color4.LightGray,
                "search.completed" => Color4.Cyan,
                "purchase.failed" => Color4.Yellow,
                "theft.completed" => Color4.Yellow,
                "sleep.started" => Color4.LightGray,
                "movement.blocked" or "door.locked" => Color4.LightGray,
                "item.picked_up" or "ai.despawned" => Color4.LightGray,
                "interaction.talk" => Color4.LightGray,
                "item.used" or "item.use.failed" or "item.dropped" or "item.wielded" or "item.examined" => Color4.LightGray,
                "needs.warning" => Color4.OrangeRed,
                "activity.completed" or "activity.cancelled" or "activity.interrupted" => Color4.LightGray,
                "interaction.examine" => Color4.LightGray,
                "container.item_taken" or "container.item_put" => Color4.LightGray,
                "character.created" => Color4.Cyan,
                _ => (Color4?)null
            };
            if (color is { } messageColor)
                log.Add(simulationEvent.Description, messageColor);
        }
    }
}

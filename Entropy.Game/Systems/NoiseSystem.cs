using Entropy.Engine.ECS.Components;
using Entropy.Game.Components;
using Entropy.Game.Components.AI;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public static class NoiseSystem
{
    public static void Emit(GameContext context, Vector2i source, int radius, string description)
    {
        var world = context.World;
        context.Events.Publish(new SimEvent(
            "noise",
            description,
            context.MapId,
            source,
            context.Clock.MinuteOfDay));
        var affected = 0;

        foreach (var entity in world.Query<Position, AIState>())
        {
            if (!world.Has<Location>(entity) || world.Get<Location>(entity).MapId != context.MapId)
                continue;

            var position = world.Get<Position>(entity).Value;
            if (Math.Abs((int)position.X - source.X) + Math.Abs((int)position.Y - source.Y) > radius)
                continue;

            ref var state = ref world.Get<AIState>(entity);
            state.Mode = AIMode.Search;
            if (world.Has<Awareness>(entity))
            {
                ref var awareness = ref world.Get<Awareness>(entity);
                awareness.LastKnownPositions[context.Player] = source;
                awareness.TurnsSinceDetected[context.Player] = 0;
            }
            affected++;
        }

        context.Log.Add($"{description}.", Color4.OrangeRed);
        if (affected > 0)
            context.Log.Add("Something nearby is investigating the noise.", Color4.Yellow);
    }
}

using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components;
using Entropy.Game.Components.AI;
using Entropy.Game.Systems;
using OpenTK.Mathematics;

namespace Entropy.Game.Behaviors;

public class ZombieBehavior : IBehavior
{
    private const int GiveUpAfterTurns = 5;
    private const float WanderChance = 0.3f;
    
    public AiIntent Decide(World world, Entity self, AiContext context)
    {
        if (!world.IsAlive(self)) return new AiIntent(AiIntentType.None);

        ref var position = ref world.Get<Position>(self).Value;
        ref var awareness = ref world.Get<Awareness>(self);

        var player = context.Player;

        ref var state = ref world.Get<AIState>(self);

        if (world.IsAlive(player) && AiUtil.IsAdjacent(position, world.Get<Position>(player).Value))
        {
            return new AiIntent(AiIntentType.Attack, Target: player);
        }

        if (awareness.Detected.ContainsKey(player))
        {
            state.Mode = AIMode.Hunt;
        }
        else if (state.Mode == AIMode.Hunt)
        {
            state.Mode = AIMode.Search;
        }

        if (awareness.TurnsSinceDetected.TryGetValue(player, out var stale)
            && stale > GiveUpAfterTurns)
        {
            state.Mode = AIMode.Idle;
        }

        switch (state.Mode)
        {
            case AIMode.Hunt:
                if (awareness.Detected.ContainsKey(player))
                {
                    var live = AiUtil.ToTile(world.Get<Position>(player).Value);
                    return new AiIntent(AiIntentType.StepToward, Destination: live);
                }
                else if (awareness.LastKnownPositions.TryGetValue(player, out var remembered))
                {
                    return new AiIntent(AiIntentType.StepToward, Destination: remembered);
                }
                else
                {
                    state.Mode = AIMode.Idle;
                }
                break;

            case AIMode.Search:
                if (awareness.LastKnownPositions.TryGetValue(player, out var lastKnown))
                {
                    var here = AiUtil.ToTile(position);
                    if (here.Equals(lastKnown))
                    {
                    return new AiIntent(AiIntentType.Wander, Chance: 0.5f);
                    }
                    else
                    {
                    return new AiIntent(AiIntentType.StepToward, Destination: lastKnown);
                    }
                }
                else
                {
                    state.Mode = AIMode.Idle;
                }
                break;

            case AIMode.Idle:
            default:
                return new AiIntent(AiIntentType.Wander, Chance: WanderChance);
        }

        return new AiIntent(AiIntentType.None);
    }

}

using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components;
using Entropy.Game.Systems;
using OpenTK.Mathematics;

namespace Entropy.Game.Behaviors;

public class ZombieBehavior : IBehavior
{
    private const int ZombieDamage = 1;
    private const int GiveUpAfterTurns = 5;
    private const float WanderChance = 0.3f;
    
    public void Act(World world, Entity self, GameContext context)
    {
        if (!world.IsAlive(self)) return;

        ref var position = ref world.Get<Position>(self).Value;
        ref var awareness = ref world.Get<Awareness>(self);

        var player = context.Player;

        // zombie state lives in the component so it survives without a class instance per zombie
        ref var state = ref world.Get<AIState>(self);

        // --- attack: adjacency means detection, regardless of mode ---
        if (world.IsAlive(player) && AiUtil.IsAdjacent(position, world.Get<Position>(player).Value))
        {
            AttackPlayer(world, context, player);
            return;
        }

        // --- sense-driven state transitions ---
        if (awareness.Detected.ContainsKey(player))
        {
            state.Mode = AIMode.Hunt;
        }
        else if (state.Mode == AIMode.Hunt)
        {
            state.Mode = AIMode.Search; // lost sight — investigate last known position
        }

        if (awareness.TurnsSinceDetected.TryGetValue(player, out var stale)
            && stale > GiveUpAfterTurns)
        {
            state.Mode = AIMode.Idle; // been too long — give up
        }

        // --- act by mode ---
        switch (state.Mode)
        {
            case AIMode.Hunt:
                if (awareness.Detected.ContainsKey(player))
                {
                    var live = AiUtil.ToTile(world.Get<Position>(player).Value);
                    AiUtil.StepToward(world, self, context.Map, live);
                }
                else if (awareness.LastKnownPositions.TryGetValue(player, out var remembered))
                {
                    AiUtil.StepToward(world, self, context.Map, remembered);
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
                        // arrived at last known spot, nobody there — hang around, then give up
                        AiUtil.Wander(world, self, context.Map, context.Rng, 0.5f);
                    }
                    else
                    {
                        AiUtil.StepToward(world, self, context.Map, lastKnown);
                    }
                }
                else
                {
                    state.Mode = AIMode.Idle;
                }
                break;

            case AIMode.Idle:
            default:
                AiUtil.Wander(world, self, context.Map, context.Rng, WanderChance);
                break;
        }
    }

    private void AttackPlayer(World world, GameContext context, Entity player)
    {
        ref var hp = ref world.Get<Health>(player);
        hp.Current -= ZombieDamage;

        if (hp.Current <= 0)
        {
            context.Log.Add("You have been killed by a zombie!", Color4.Red);
            return;
        }

        context.Log.Add($"The zombie hits you for {ZombieDamage}!", Color4.OrangeRed);
        context.Log.Add($"You feel that wicked pain in your chest as the zombie's claws tear through your flesh... HP {hp.Current}/{hp.Max}", Color4.OrangeRed);
    }

}
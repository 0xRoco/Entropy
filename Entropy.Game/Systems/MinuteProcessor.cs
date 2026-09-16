using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Behaviors;
using Entropy.Game.Components.Vitals;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public sealed class MinuteProcessor(IGameRuntimeContext context, TurnProcessor turns)
{
    public bool Process(SimulationState state)
    {
        NeedsSystem.Update(context);
        turns.RunAITurns(state, context.Visibilities,
            (actor, intent, aiContext) => ExecuteAiIntent(context, actor, intent, aiContext));
        if (state.World.IsAlive(state.Player) && ActivitySystem.IsActive(state.World, state.Player))
            ActivitySystem.Advance(context, state.Player);

        return state.World.IsAlive(state.Player) &&
               (!state.World.Has<Health>(state.Player) ||
                state.World.Get<Health>(state.Player).Current > 0);
    }

    private static ActionResult ExecuteAiIntent(IGameRuntimeContext context, Entity actor, AiIntent intent,
        AiContext aiContext)
    {
        var world = aiContext.World;
        switch (intent.Type)
        {
            case AiIntentType.Attack when intent.Target is { } target && world.IsAlive(target):
                return InteractionSystem.Attack(context, actor, target);
            case AiIntentType.StepToward when intent.Destination is { } destination:
                return AiUtil.StepToward(world, actor, aiContext.Map, destination)
                    ? ActionResult.Turn
                    : ActionResult.Failed;
            case AiIntentType.Wander:
                return AiUtil.Wander(world, actor, aiContext.Map, aiContext.Rng, intent.Chance)
                    ? ActionResult.Turn
                    : ActionResult.Failed;
            case AiIntentType.WanderNear when intent.Anchor is { } anchor:
                return AiUtil.WanderNear(world, actor, aiContext.Map, aiContext.Rng, anchor, intent.Radius,
                    intent.Chance)
                    ? ActionResult.Turn
                    : ActionResult.Failed;
            case AiIntentType.TravelToward when intent.MapId != null && intent.Destination is { } destination:
                return AiUtil.TravelToward(world, actor, context.Maps, intent.MapId, destination)
                    ? ActionResult.Turn
                    : ActionResult.Failed;
            case AiIntentType.Arrest when intent.Target is { } target && world.IsAlive(target):
                return RespondBehavior.ExecuteArrest(world, actor, target, context);
            case AiIntentType.Despawn:
                var position = world.Get<Position>(actor).Value;
                context.Events.Publish(new SimEvent(
                    "ai.despawned",
                    "The officer shrugs and leaves.",
                    world.Get<Location>(actor).MapId,
                    new Vector2i((int)position.X, (int)position.Y),
                    context.Clock.MinuteOfDay,
                    world.StableId(actor)));
                world.Destroy(actor);
                return ActionResult.Turn;
            default:
                return ActionResult.Failed;
        }
    }
}

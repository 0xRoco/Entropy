using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components;
using Entropy.Game.Behaviors;
using Entropy.Game.Components.AI;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Simulation;
using Entropy.Game.Components.Spatial;
using Entropy.Game.Components.Tags;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public class TurnProcessor
{
    private const int ActionCost = 100;
    private const int DefaultSpeed = 100;

    private readonly List<Entity> _actors = [];
    private readonly Dictionary<Entity, int> _energy = [];

    public void AddActor(Entity entity)
    {
        if (_actors.Contains(entity)) return;
        _actors.Add(entity);
        _energy[entity] = 0;
    }

    public void RemoveActor(Entity entity)
    {
        _actors.Remove(entity);
        _energy.Remove(entity);
    }

    public ActionResult ProcessPlayerTurn(
        Entity player,
        Vector2i move,
        GameContext context,
        VisibilityMap visibility,
        int viewRadius)
    {
        ref var pos = ref context.World.Get<Position>(player);
        var target = pos.Value + move;
        var tx = (int)target.X;
        var ty = (int)target.Y;

        if (tx < 0 || tx >= context.Map.Width || ty < 0 || ty >= context.Map.Height)
            return ActionResult.Failed;

        if (!context.Map[tx, ty].Walkable)
            return ActionResult.Failed;

        var playerMapId = context.World.Get<Location>(player).MapId;
        var playerNameOf = (Entity entity) => context.World.Has<Named>(entity)
            ? context.World.Get<Named>(entity).Name
            : "something";

        foreach (var entity in context.World.Query<Position, Actor>())
        {
            if (entity.Equals(player)) continue;
            if (context.World.Get<Position>(entity).Value != target) continue;
            if (!context.World.Has<Location>(entity) ||
                context.World.Get<Location>(entity).MapId != playerMapId)
                continue;

            if (context.World.Has<Hostile>(entity))
            {
                InteractionSystem.Attack(context, player, entity);
                Spend(player);
                return ActionResult.Turn;
            }

            context.Log.Add($"The {playerNameOf(entity)} blocks your way", Color4.LightGray);
            Spend(player);
            return ActionResult.Turn;
        }

        foreach (var entity in context.World.Query<Position, Solid>())
        {
            if (!context.World.Get<Solid>(entity).Blocks) continue;
            if (context.World.Get<Position>(entity).Value != target) continue;
            if (!context.World.Has<Location>(entity) ||
                context.World.Get<Location>(entity).MapId != playerMapId)
                continue;

            context.Log.Add($"The {playerNameOf(entity)} blocks your way", Color4.LightGray);
            Spend(player);
            return ActionResult.Turn;
        }

        var transition = context.Maps.TransitionAt(playerMapId, new Vector2i(tx, ty));

        if (transition != null)
        {
            if (!DoorSystem.IsPassable(context, transition))
            {
                context.Log.Add("The door is locked.", Color4.Yellow);
                return ActionResult.Failed;
            }

            pos.Value = target;
            context.World.Set(player, new Facing { Direction = new Vector2i(move.X, move.Y) });

            pos.Value = new Vector2(transition.ToTile.X, transition.ToTile.Y);

            context.World.Set(player, new Location
            {
                MapId = transition.ToMap
            });

            context.MapId = transition.ToMap;
            context.Map = context.Maps[transition.ToMap];
            context.Visibility = context.Visibilities[transition.ToMap];

            if (DoorSystem.TryGet(context, transition, out var door, out var doorState) &&
                door.Trespass && doorState.Broken && !doorState.TrespassReported)
            {
                doorState.TrespassReported = true;
                context.DoorStates[DoorSystem.KeyFor(transition)] = doorState;
                var victim = context.World.Query<Home>()
                    .FirstOrDefault(entity => context.World.Has<Home>(entity) &&
                        context.World.Get<Home>(entity).MapId == transition.ToMap);
                ConsequenceSystem.Report(context, "trespass", "someone forced entry into a home",
                    transition.ToTile, context.Player, victim);
            }

            Fov.Compute(
                transition.ToTile,
                viewRadius,
                context.Map,
                context.Visibility);

            Spend(
                player,
                context.Map[transition.ToTile.X, transition.ToTile.Y].MoveCost);

            return ActionResult.Turn;
        }

        pos.Value = target;
        context.World.Set(player, new Facing { Direction = new Vector2i(move.X, move.Y) });

        Fov.Compute(new Vector2i(tx, ty), viewRadius, context.Map, visibility);
        Spend(player, context.Map[tx, ty].MoveCost);
        return ActionResult.Turn;
    }

    public void RunAITurns(Entity player, GameContext ctx)
    {
        for (var i = _actors.Count - 1; i >= 0; i--)
            if (!ctx.World.IsAlive(_actors[i]))
                RemoveActor(_actors[i]);

        var perceivers = ctx.World.Query<Position, Perception>()
            .Where(entity =>
                ctx.World.Has<Location>(entity) &&
                ctx.World.Get<Location>(entity).MapId == ctx.MapId)
            .ToList();

        var candidates = ctx.World.Query<Position, Actor>()
            .Where(entity =>
                ctx.World.Has<Location>(entity) &&
                ctx.World.Get<Location>(entity).MapId == ctx.MapId)
            .ToList();

        PerceptionSystem.Update(ctx.World, ctx.Map, perceivers, candidates);

        foreach (var actor in _actors)
            _energy[actor] = _energy.GetValueOrDefault(actor) + SpeedOf(ctx, actor);

        var acted = true;
        while (acted)
        {
            acted = false;
            foreach (var actor in _actors.ToList())
            {
                if (actor.Equals(player) || !ctx.World.IsAlive(actor)) continue;
                if (_energy.GetValueOrDefault(actor) < ActionCost) continue;

                _energy[actor] -= ActionCost;
                if (ctx.World.Has<Behavior>(actor))
                {
                    var behaviorId = ctx.World.Get<Behavior>(actor).BehaviorId;
                    var intent = BehaviorCatalog.Get(behaviorId).Decide(ctx.World, actor, ctx);
                    _ = ExecuteAiIntent(actor, intent, ctx);
                }

                if (!ctx.World.IsAlive(player)) return;
                acted = true;
            }
        }
    }

    private void Spend(Entity entity, int amount = 100)
    {
        if (_energy.ContainsKey(entity))
            _energy[entity] -= amount;
    }

    private static int SpeedOf(GameContext ctx, Entity entity) =>
        ctx.World.Has<Speed>(entity) ? ctx.World.Get<Speed>(entity).Value : DefaultSpeed;

    private static ActionResult ExecuteAiIntent(Entity actor, AiIntent intent, GameContext context)
    {
        var world = context.World;
        switch (intent.Type)
        {
            case AiIntentType.Attack when intent.Target is { } target && world.IsAlive(target):
                return InteractionSystem.Attack(context, actor, target);
            case AiIntentType.StepToward when intent.Destination is { } destination:
                return AiUtil.StepToward(world, actor, context.Map, destination)
                    ? ActionResult.Turn
                    : ActionResult.Failed;
            case AiIntentType.Wander:
                return AiUtil.Wander(world, actor, context.Map, context.Rng, intent.Chance)
                    ? ActionResult.Turn
                    : ActionResult.Failed;
            case AiIntentType.WanderNear when intent.Anchor is { } anchor:
                return AiUtil.WanderNear(world, actor, context.Map, context.Rng, anchor, intent.Radius, intent.Chance)
                    ? ActionResult.Turn
                    : ActionResult.Failed;
            case AiIntentType.TravelToward when intent.MapId != null && intent.Destination is { } destination:
                return AiUtil.TravelToward(world, actor, context, intent.MapId, destination)
                    ? ActionResult.Turn
                    : ActionResult.Failed;
            case AiIntentType.Arrest when intent.Target is { } target && world.IsAlive(target):
                return RespondBehavior.ExecuteArrest(world, actor, target, context);
            case AiIntentType.Despawn:
                context.Log.Add("The officer shrugs and leaves.", Color4.LightGray);
                world.Destroy(actor);
                return ActionResult.Turn;
            default:
                return ActionResult.Failed;
        }
    }
}

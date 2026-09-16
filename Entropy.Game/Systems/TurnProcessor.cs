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
using Entropy.Simulation;

namespace Entropy.Game.Systems;

public class TurnProcessor
{
    private const int ActionCost = 100;
    private const int DefaultSpeed = 100;

    private readonly ActorScheduler _scheduler;

    public ActorScheduler Scheduler => _scheduler;

    public TurnProcessor(ActorScheduler? scheduler = null) => _scheduler = scheduler ?? new ActorScheduler();

    public void AddActor(Entity entity)
    {
        _scheduler.Add(entity);
    }

    public void RemoveActor(Entity entity)
    {
        _scheduler.Remove(entity);
    }

    public void RunAITurns(
        Entropy.Simulation.SimulationState state,
        IReadOnlyDictionary<string, VisibilityMap> visibilities,
        Func<Entity, AiIntent, AiContext, ActionResult> executeIntent)
    {
        var world = state.World;
        _scheduler.RemoveDead(world);

        _scheduler.AddEnergy(actor => SpeedOf(state, actor));

        var mapIds = _scheduler.Actors
            .Where(world.IsAlive)
            .Where(world.Has<Location>)
            .Select(actor => world.Get<Location>(actor).MapId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var mapId in mapIds)
        {
            if (!state.Maps.Maps.ContainsKey(mapId) || !visibilities.ContainsKey(mapId))
                continue;

            RunActorsOnMap(state, visibilities, executeIntent, mapId);
            if (!world.IsAlive(state.Player))
                return;
        }
    }

    private void RunActorsOnMap(
        Entropy.Simulation.SimulationState state,
        IReadOnlyDictionary<string, VisibilityMap> visibilities,
        Func<Entity, AiIntent, AiContext, ActionResult> executeIntent,
        string mapId)
    {
        var world = state.World;
        var aiContext = new AiContext
        {
            World = world,
            Player = state.Player,
            Maps = state.Maps,
            Rng = state.Rng,
            Clock = state.Clock,
            MapId = mapId,
            Map = state.Maps[mapId],
            Visibility = visibilities[mapId]
        };

        var perceivers = world.Query<Position, Perception>()
            .Where(entity =>
                world.Has<Location>(entity) &&
                world.Get<Location>(entity).MapId.Equals(mapId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var candidates = world.Query<Position, Actor>()
            .Where(entity =>
                world.Has<Location>(entity) &&
                world.Get<Location>(entity).MapId.Equals(mapId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        PerceptionSystem.Update(world, aiContext.Map, perceivers, candidates);

        _scheduler.RunReady(
            state.Player,
            ActionCost,
            actor => world.IsAlive(actor) &&
                     world.Has<Location>(actor) &&
                     world.Get<Location>(actor).MapId
                         .Equals(mapId, StringComparison.OrdinalIgnoreCase),
            actor => ExecuteAiActor(actor, aiContext, executeIntent),
            () => !world.IsAlive(state.Player));
    }

    private static ActionResult ExecuteAiActor(
        Entity actor,
        AiContext aiContext,
        Func<Entity, AiIntent, AiContext, ActionResult> executeIntent)
    {
        var world = aiContext.World;
        if (!world.Has<Behavior>(actor))
            return ActionResult.Free;

        var behaviorId = world.Get<Behavior>(actor).BehaviorId;
        var intent = BehaviorCatalog.Get(behaviorId).Decide(world, actor, aiContext);
        return executeIntent(actor, intent, aiContext);
    }

    private void Spend(Entity entity, int amount = 100)
    {
        _scheduler.Spend(entity, amount);
    }

    private static int SpeedOf(Entropy.Simulation.SimulationState state, Entity entity) =>
        state.World.Has<Speed>(entity) ? state.World.Get<Speed>(entity).Value : DefaultSpeed;
}

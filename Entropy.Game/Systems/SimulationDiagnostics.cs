using Entropy.Engine.ECS.Components;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Simulation;
using Entropy.Game.Components.Vitals;

namespace Entropy.Game.Systems;

public sealed record NpcSnapshot(
    long StableId,
    string? Name,
    string MapId,
    int X,
    int Y,
    int? Health,
    int? Hunger,
    int? Fatigue,
    ActivityKind? Activity,
    int SchedulerEnergy);

public static class SimulationDiagnostics
{
    public static IReadOnlyList<NpcSnapshot> Npcs(IGameRuntimeContext context)
    {
        return
        [
            .. context.World.Query<Location, Position>()
                .Where(entity => !entity.Equals(context.Player))
                .Select(entity =>
                {
                    var position = context.World.Get<Position>(entity).Value;
                    return new NpcSnapshot(
                        context.World.StableId(entity),
                        context.World.Has<Named>(entity) ? context.World.Get<Named>(entity).Name : null,
                        context.World.Get<Location>(entity).MapId,
                        (int)position.X,
                        (int)position.Y,
                        context.World.Has<Health>(entity) ? context.World.Get<Health>(entity).Current : null,
                        context.World.Has<Hunger>(entity) ? context.World.Get<Hunger>(entity).Current : null,
                        context.World.Has<Fatigue>(entity) ? context.World.Get<Fatigue>(entity).Current : null,
                        context.World.Has<Activity>(entity) ? context.World.Get<Activity>(entity).Kind : null,
                        context.Scheduler.EnergyOf(entity));
                })
                .OrderBy(snapshot => snapshot.StableId)
        ];
    }

    public static IReadOnlyList<SimEvent> EventHistory(IGameRuntimeContext context) => context.Events.History;
}
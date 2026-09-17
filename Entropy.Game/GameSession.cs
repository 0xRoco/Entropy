using Entropy.Engine.Core;
using Entropy.Engine.World;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using Entropy.Game.Components.Spatial;
using Entropy.Game.WorldGen;

namespace Entropy.Game;

public sealed class GameSession
{
    private GameSession(
        GameContext context,
        ISimulation simulation,
        Dictionary<string, VisibilityMap> visibilities,
        IReadOnlyDictionary<string, BuildingInstance> buildings)
    {
        Context = context;
        Simulation = simulation;
        Visibilities = visibilities;
        Buildings = buildings;
    }

    public GameContext Context { get; }
    public ISimulation Simulation { get; }
    public Dictionary<string, VisibilityMap> Visibilities { get; }
    public IReadOnlyDictionary<string, BuildingInstance> Buildings { get; }
    public int ViewRadius => Context.ViewRadius;

    public static GameSession Create(
        WorldSetup.NewGameResult result,
        MessageLog log,
        DefinitionRegistry definitions,
        Rng rng,
        int viewRadius)
    {
        var simulation = result.Simulation;
        var entryTransition = result.Maps.Transitions.SingleOrDefault(transition =>
            transition.FromMap.Equals("checkpoint", StringComparison.OrdinalIgnoreCase) &&
            transition.ToMap.Equals("spire_commons", StringComparison.OrdinalIgnoreCase));
        var doorDefinitions = new Dictionary<DoorKey, DoorDefinition>();
        var doorStates = new Dictionary<DoorKey, DoorState>();
        if (entryTransition is not null)
        {
            var doorKey = DoorSystem.KeyFor(entryTransition);
            doorDefinitions[doorKey] = new DoorDefinition(
                "spire_entry_gate",
                string.Empty,
                string.Empty,
                RequiresTemporaryPermit: true);
            doorStates[doorKey] = new DoorState { Locked = true };
        }
        var visibility = result.Visibilities[result.MapId];
        var context = new GameContext(simulation)
        {
            Map = result.Map,
            MapId = result.MapId,
            Log = log,
            Visibilities = result.Visibilities,
            Visibility = visibility,
            ViewRadius = viewRadius,
            DoorDefinitions = doorDefinitions,
            DoorStates = doorStates,
        };
        var adapter = new SimulationRuntime(simulation, context);
        return new GameSession(context, adapter, result.Visibilities, result.Buildings);
    }

    public void SetMap(string mapId)
    {
        if (!Context.Maps.Maps.TryGetValue(mapId, out var map))
            throw new ArgumentException($"Unknown map '{mapId}'.", nameof(mapId));

        Context.MapId = mapId;
        Context.Map = map;
        Context.Visibility = Visibilities[mapId];
    }

    public GameSaveData CaptureSave() => GameSave.Capture(Context);

    public void RestoreSave(GameSaveData save)
    {
        GameSave.RestorePlayer(Context, save);
        SetMap(save.MapId);
        var position = Context.World.Get<Entropy.Engine.ECS.Components.Position>(Context.Player).Value;
        Fov.Compute(new OpenTK.Mathematics.Vector2i((int)position.X, (int)position.Y), ViewRadius, Context.Map,
            Context.Visibility);
    }
}

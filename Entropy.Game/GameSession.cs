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
        var pharmacyTransition = result.Maps.Transitions.Single(transition =>
            transition.ToMap.Equals("neighborhood_pharmacy_interior", StringComparison.OrdinalIgnoreCase));
        var pharmacyDoor = DoorSystem.KeyFor(pharmacyTransition);
        var simulation = result.Simulation;
        var visibility = result.Visibilities[result.MapId];
        var turns = new TurnProcessor(simulation.Scheduler);
        var context = new GameContext
        {
            Map = result.Map,
            MapId = result.MapId,
            Maps = result.Maps,
            Log = log,
            World = result.World,
            Definitions = definitions,
            Player = result.Player,
            Rng = rng,
            Clock = simulation.Clock,
            Scheduler = simulation.Scheduler,
            Visibilities = result.Visibilities,
            Visibility = visibility,
            ViewRadius = viewRadius,
            DoorDefinitions = new()
            {
                [pharmacyDoor] = new DoorDefinition("pharmacy_door", "key_pharmacy", "tool_smash", Trespass: false)
            },
            DoorStates = new()
            {
                [pharmacyDoor] = new DoorState { Locked = true }
            },
            Events = simulation.Events,
            Simulation = simulation
        };
        var adapter = new SimulationRuntime(simulation, context, log);
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

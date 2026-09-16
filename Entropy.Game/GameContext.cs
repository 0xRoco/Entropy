using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.World;
using Entropy.Content;
using Entropy.Game.Components.Spatial;
using Entropy.Game.Systems;
using Entropy.Game.UI;
using Entropy.Simulation;

namespace Entropy.Game;

public sealed class GameContext : IGameRuntimeContext
{
    private SimulationState? _simulation;
    private MapGraph? _maps;
    private World? _world;
    private DefinitionRegistry? _definitions;
    private Rng? _rng;
    private Entropy.Engine.ECS.Entity _player;
    private WorldClock? _clock;
    private SimulationEventBus? _events;

    public GameContext() { }

    public GameContext(SimulationState simulation) => _simulation = simulation;

    public required TileMap Map { get; set; }
    public required string MapId { get; set; }
    public MapGraph Maps
    {
        get => Simulation.Maps;
        init => _maps = value;
    }
    public required MessageLog Log { get; init; }
    public World World
    {
        get => Simulation.World;
        init => _world = value;
    }
    public DefinitionRegistry Definitions
    {
        get => Simulation.Definitions;
        init => _definitions = value;
    }
    public Rng Rng
    {
        get => Simulation.Rng;
        init => _rng = value;
    }
    public Entropy.Engine.ECS.Entity Player
    {
        get => Simulation.Player;
        init => _player = value;
    }
    public WorldClock Clock
    {
        get => Simulation.Clock;
        init => _clock = value;
    }
    public ActorScheduler Scheduler
    {
        get => Simulation.Scheduler;
        init => _scheduler = value;
    }
    public required Dictionary<string, VisibilityMap> Visibilities { get; init; }
    public required VisibilityMap Visibility { get; set; }
    public required int ViewRadius { get; init; }
    public required Dictionary<DoorKey, DoorDefinition> DoorDefinitions { get; init; }
    public required Dictionary<DoorKey, DoorState> DoorStates { get; init; }
    public SimulationEventBus Events
    {
        get => Simulation.Events;
        init => _events = value;
    }

    public SimulationState Simulation
    {
        get => _simulation ??= BuildSimulation();
        init => _simulation = value;
    }

    private SimulationState BuildSimulation()
    {
        if (_maps is null || _world is null || _definitions is null || _rng is null || _clock is null)
            throw new InvalidOperationException("Simulation state is not initialized.");

        return new SimulationState(new SimulationContext
        {
            World = _world,
            Maps = _maps,
            Definitions = _definitions,
            Rng = _rng,
            Player = _player,
            Clock = _clock,
            Events = _events ??= new SimulationEventBus(),
            Scheduler = _scheduler ?? throw new InvalidOperationException("Scheduler is not initialized.")
        });
    }

    private ActorScheduler? _scheduler;
}

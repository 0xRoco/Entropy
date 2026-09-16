using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.World;
using Entropy.Content;
using Entropy.Game.Components.Spatial;
using Entropy.Game.Systems;
using Entropy.Game.UI;

namespace Entropy.Game;

public sealed class GameContext : IGameRuntimeContext
{
    private World? _world;
    private MapGraph? _maps;
    private DefinitionRegistry? _definitions;
    private Rng? _rng;
    private Entity _player;
    private WorldClock? _clock;
    private SimulationEventBus? _events;

    public required TileMap Map { get; set; }
    public required string MapId { get; set; }
    public required MapGraph Maps
    {
        get => Simulation?.Maps ?? _maps ?? throw new InvalidOperationException("Maps are not initialized.");
        init => _maps = value;
    }
    public required MessageLog Log { get; init; }
    public required World World
    {
        get => Simulation?.World ?? _world ?? throw new InvalidOperationException("World is not initialized.");
        init => _world = value;
    }
    public required DefinitionRegistry Definitions
    {
        get => Simulation?.Definitions ?? _definitions ?? throw new InvalidOperationException("Definitions are not initialized.");
        init => _definitions = value;
    }
    public required Rng Rng
    {
        get => Simulation?.Rng ?? _rng ?? throw new InvalidOperationException("RNG is not initialized.");
        init => _rng = value;
    }
    public required Entity Player
    {
        get => Simulation?.Player ?? _player;
        init => _player = value;
    }
    public required WorldClock Clock
    {
        get => Simulation?.Clock ?? _clock ?? throw new InvalidOperationException("Clock is not initialized.");
        init => _clock = value;
    }
    public required ActorScheduler Scheduler { get; init; }
    public required Dictionary<string, VisibilityMap> Visibilities { get; init; }
    public required VisibilityMap Visibility { get; set; }
    public required int ViewRadius { get; init; }
    public required Dictionary<DoorKey, DoorDefinition> DoorDefinitions { get; init; }
    public required Dictionary<DoorKey, DoorState> DoorStates { get; init; }
    public SimulationEventBus Events
    {
        get => Simulation?.Events ?? (_events ??= new SimulationEventBus());
        init => _events = value;
    }
    public Entropy.Simulation.SimulationState? Simulation { get; set; }
}

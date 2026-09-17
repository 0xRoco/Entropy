using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.World;
using Entropy.Game.Components.Spatial;
using Entropy.Game.Components;

namespace Entropy.Game.Systems;

public interface IGameRuntimeContext
{
    TileMap Map { get; set; }
    string MapId { get; set; }
    MapGraph Maps { get; }
    World World { get; }
    DefinitionRegistry Definitions { get; }
    Rng Rng { get; }
    Entity Player { get; }
    WorldClock Clock { get; }
    ActorScheduler Scheduler { get; }
    Dictionary<string, VisibilityMap> Visibilities { get; }
    VisibilityMap Visibility { get; set; }
    int ViewRadius { get; }
    Dictionary<DoorKey, DoorDefinition> DoorDefinitions { get; }
    Dictionary<DoorKey, DoorState> DoorStates { get; }
    SimulationEventBus Events { get; }
    ArrivalState Arrival { get; }
}

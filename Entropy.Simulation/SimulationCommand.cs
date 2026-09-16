using OpenTK.Mathematics;
using Entropy.Engine.ECS;
using Entropy.Engine.World;

namespace Entropy.Simulation;

public abstract record SimulationCommand;

public sealed record MoveCommand(Vector2i Direction) : SimulationCommand;

public sealed record PickupCommand : SimulationCommand;

public sealed record WaitCommand : SimulationCommand;

public sealed record ExamineCommand(Vector2i Tile) : SimulationCommand;

public sealed record ExamineEntityCommand(Entity Target) : SimulationCommand;

public sealed record PickupItemCommand(Entity Item) : SimulationCommand;

public sealed record TalkCommand(Entity Target) : SimulationCommand;

public sealed record SearchCommand(Entity Container) : SimulationCommand;

public sealed record SleepCommand(Entity Bed) : SimulationCommand;

public sealed record AttackCommand(Entity Target) : SimulationCommand;

public sealed record PurchaseCommand(Entity Item, Entity Shop) : SimulationCommand;

public sealed record StealCommand(Entity Item, Entity Shop) : SimulationCommand;

public sealed record UnlockDoorCommand(MapTransition Transition) : SimulationCommand;

public sealed record ForceDoorCommand(
    MapTransition Transition,
    Vector2i Source,
    string ToolFlag,
    string Method,
    int NoiseRadius) : SimulationCommand;

public sealed record UnlockContainerCommand(Entity Container) : SimulationCommand;

public sealed record ForceContainerCommand(
    Entity Container,
    Vector2i Source,
    string ToolFlag,
    string Method,
    int NoiseRadius) : SimulationCommand;

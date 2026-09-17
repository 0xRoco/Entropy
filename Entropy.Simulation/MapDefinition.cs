namespace Entropy.Simulation;

public sealed class MapDefinition
{
    public int Version { get; init; } = 1;
    public required string Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Kind { get; init; } = "authored";
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required MapTerrainLayer Terrain { get; init; }
    public List<MapObjectPlacement> Objects { get; init; } = [];
    public List<MapAnchor> Anchors { get; init; } = [];
    public List<MapTransitionDefinition> Transitions { get; init; } = [];
}

public sealed class MapTerrainLayer
{
    public required Dictionary<string, string> Legend { get; init; }
    public required List<string> Rows { get; init; }
}

public sealed class MapObjectPlacement
{
    public required string Id { get; init; }
    public required string Definition { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public int Rotation { get; init; }
}

public sealed class MapAnchor
{
    public required string Id { get; init; }
    public required string Kind { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
}

public sealed class MapTransitionDefinition
{
    public required string Id { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required string TargetMap { get; init; }
    public required string TargetAnchor { get; init; }
}

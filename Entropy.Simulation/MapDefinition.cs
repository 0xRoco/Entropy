namespace Entropy.Simulation;

public sealed class MapDefinition
{
    public int Version { get; init; } = 1;
    public required string Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Kind { get; init; } = "authored";
    public required int Width { get; set; }
    public required int Height { get; set; }
    public required MapTerrainLayer Terrain { get; init; }
    public List<MapObjectPlacement> Objects { get; init; } = [];
    public List<MapAnchor> Anchors { get; init; } = [];
    public List<MapTransitionDefinition> Transitions { get; init; } = [];
}

public sealed class MapTerrainLayer
{
    public required Dictionary<string, string> Legend { get; init; }
    public required List<string> Rows { get; set; }
}

public sealed class MapObjectPlacement
{
    public required string Id { get; init; }
    public required string Definition { get; set; }
    public required int X { get; set; }
    public required int Y { get; set; }
    public int Rotation { get; init; }
}

public sealed class MapAnchor
{
    public required string Id { get; init; }
    public required string Kind { get; set; }
    public required int X { get; set; }
    public required int Y { get; set; }
}

public sealed class MapTransitionDefinition
{
    public required string Id { get; init; }
    public required int X { get; set; }
    public required int Y { get; set; }
    public required string TargetMap { get; set; }
    public required string TargetAnchor { get; set; }
}

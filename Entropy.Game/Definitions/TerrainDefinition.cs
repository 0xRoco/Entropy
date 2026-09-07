using OpenTK.Mathematics;

namespace Entropy.Game.Definitions;

public class TerrainDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required char Symbol { get; init; }
    public required Color4 Color { get; init; }
    public required Color4 Background { get; init; }
    public bool Walkable { get; init; } = true;
    public bool Opaque { get; init; }
    public int MoveCost { get; init; } = 100;
    public HashSet<string> Flags { get; init; } = [];

    public bool HasFlag(string flag) => Flags.Contains(flag);
}

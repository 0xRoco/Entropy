using OpenTK.Mathematics;

namespace Entropy.Content;

public class TerrainDefinition
{
    public string? Comment { get; set; }
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required char Symbol { get; set; }
    public required Color4 Color { get; set; }
    public required Color4 Background { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool Walkable { get; set; } = true;
    public bool Opaque { get; set; }
    public int MoveCost { get; set; } = 100;
    public HashSet<string> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool HasFlag(string flag) => Flags.Contains(flag);
}

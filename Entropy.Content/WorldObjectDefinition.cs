using OpenTK.Mathematics;

namespace Entropy.Content;

public class WorldObjectDefinition
{
    public string? Comment { get; set; }
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required char Symbol { get; set; }
    public required Color4 Color { get; set; }
    public string Description { get; set; } = string.Empty;
    public HashSet<string> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int ContainerSlots { get; set; }
    public List<string> StarterItems { get; set; } = [];

    public bool HasFlag(string flag) => Flags.Contains(flag);
    public bool IsContainer => ContainerSlots > 0;
}

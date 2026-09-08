using OpenTK.Mathematics;

namespace Entropy.Content;

public class ItemDefinition
{
    public string? Comment { get; set; }
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required char Symbol { get; set; }
    public required Color4 Color { get; set; }

    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public List<Material> Materials { get; set; } = [];
    public HashSet<string> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public int Weight { get; set; }

    public bool Stackable { get; set; }
    public int MaxStack { get; set; } = 10;

    public List<ItemEffect> Effects { get; set; } = [];

    public T? Effect<T>() where T : ItemEffect =>
        Effects.OfType<T>().FirstOrDefault();
}

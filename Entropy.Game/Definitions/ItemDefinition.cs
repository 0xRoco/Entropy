using OpenTK.Mathematics;

namespace Entropy.Game.Definitions;

public class ItemDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required char Symbol { get; init; }
    public required Color4 Color { get; init; }
    
    public string? Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    
    public List<Material> Materials { get; init; } = [];
    public HashSet<string> Flags { get; init; } = [];
    
    public int Weight { get; init; }
    
    public bool Stackable { get; init; }
    public int MaxStack { get; init; } = 10;
    
    public List<ItemEffect> Effects { get; init; } = [];
    
    public T? Effect<T>() where T : ItemEffect =>
        Effects.OfType<T>().FirstOrDefault();
}
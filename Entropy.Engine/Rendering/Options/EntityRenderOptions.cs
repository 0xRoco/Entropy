namespace Entropy.Engine.Rendering.Options;

public class EntityRenderOptions
{
    public static readonly EntityRenderOptions Default = new();
    public bool CullByVisibility { get; init; } = true; // Hide entities that are not visible
    public float MemoryDim { get; init; } = 0.3f;       // Dim factor for explored but not visible entities
}
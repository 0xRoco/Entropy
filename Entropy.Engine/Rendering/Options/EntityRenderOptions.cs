namespace Entropy.Engine.Rendering.Options;

public class EntityRenderOptions
{
    public static readonly EntityRenderOptions Default = new();
    public bool CullByVisibility { get; init; } = true; // Hide entities that are not visible
}
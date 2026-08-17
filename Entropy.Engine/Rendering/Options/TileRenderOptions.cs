namespace Entropy.Engine.Rendering.Options;

public class TileRenderOptions
{
    public static readonly TileRenderOptions Default = new();
    public float MemoryDim { get; init; } = 0.3f; // Explored not visible dimming
    public bool RenderUnexplored { get; init; } = false; // Render unexplored tiles
}
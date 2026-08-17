using Entropy.Engine.Rendering.Options;
using Entropy.Engine.World;
using OpenTK.Mathematics;

namespace Entropy.Engine.Rendering;

public static class TileRenderer
{
    public static void Draw(TileMap map, VisibilityMap visibility, Camera camera, QuadBatcher batcher, TileRenderOptions? option = null)
    {
        var options = option ?? new TileRenderOptions();
        
        var (min, max) = camera.VisibleWorldBounds();
        var xMin = Math.Max(0, (int)Math.Floor(min.X));
        var xMax = Math.Min(map.Width - 1, (int)Math.Ceiling(max.X));
        var yMin = Math.Max(0, (int)Math.Floor(min.Y));
        var yMax = Math.Min(map.Height - 1, (int)Math.Ceiling(max.Y));
        
        for (var y = yMin; y <= yMax; y++)
        for (var x = xMin; x <= xMax; x++)
        {
            if (!visibility.IsExplored(x, y))
            {
                if (options.RenderUnexplored)
                {
                    //TODO: Render unexplored tiles (e.g., as black or fog)
                }
                continue;
            }

            var tile = map[x, y];
            var visible = visibility.IsVisible(x, y);
            
            var bg = visible ? tile.Background : tile.Background.Scaled(options.MemoryDim);
            var fg = visible ? tile.Foreground : tile.Foreground.Scaled(options.MemoryDim);

            batcher.AddRect(x, y, 1, 1, bg);
            batcher.AddTexturedQuad(x, y, 1, 1, fg, tile.Glyph);
        }
        batcher.Flush();
    }
}
using Entropy.Engine.Rendering.Options;
using Entropy.Engine.World;
using OpenTK.Mathematics;

namespace Entropy.Engine.Rendering;

public static class TileRenderer
{
    public static void Draw(
        TileMap map,
        VisibilityMap visibility,
        Camera camera,
        QuadBatcher batcher,
        TileRenderOptions? option = null,
        string tilesetMode = "ascii",
        GlyphAtlas? artAtlas = null,
        int artCellSize = 16,
        IReadOnlyDictionary<string, Vector2i>? spriteMap = null,
        IReadOnlyList<string?>? terrainSpriteKeys = null)
    {
        var options = option ?? new TileRenderOptions();
        var artMode = tilesetMode == "art" && artAtlas != null;

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
                    // TODO: render unexplored tiles
                }
                continue;
            }

            var tile = map[x, y];
            var visible = visibility.IsVisible(x, y);

            if (artMode)
            {
                var tint = visible
                    ? Color4.White
                    : Color4.White.Scaled(options.MemoryDim);

                DrawArtTile(batcher, artAtlas!, tile, x, y, tint, artCellSize,
                    spriteMap, terrainSpriteKeys);
                continue;
            }

            var bg = visible ? tile.Background : tile.Background.Scaled(options.MemoryDim);
            var fg = visible ? tile.Foreground : tile.Foreground.Scaled(options.MemoryDim);

            batcher.AddRect(x, y, 1, 1, bg);
            batcher.AddTexturedQuad(x, y, 1, 1, fg, tile.Glyph);
        }

        batcher.Flush();
    }

    private static void DrawArtTile(
        QuadBatcher batcher,
        GlyphAtlas atlas,
        Tile tile,
        int x,
        int y,
        Color4 tint,
        int cellSize,
        IReadOnlyDictionary<string, Vector2i>? spriteMap,
        IReadOnlyList<string?>? terrainSpriteKeys)
    {
        // index 0 = legacy/default tiles with no art sprite
        if (tile.TerrainDefIndex == 0)
        {
            batcher.AddRect(x, y, 1, 1, tint.Scaled(0.25f) with { A = 1f });
            return;
        }

        var index = tile.TerrainDefIndex - 1;

        // Preferred: named sprite ("terrain:<id>") from the tileset's sprite
        // map, resolved via the terrain def's ordered sprite-key list.
        if (spriteMap is not null && terrainSpriteKeys is not null &&
            index < terrainSpriteKeys.Count &&
            terrainSpriteKeys[index] is { } key &&
            spriteMap.TryGetValue(key, out var cell))
        {
            AddAtlasQuad(batcher, atlas, cell.X, cell.Y, cellSize, x, y, tint);
            return;
        }

        // Fallback: legacy sequential layout (def order, row-major).
        var cellsAcross = Math.Max(1, atlas.Width / cellSize);
        AddAtlasQuad(batcher, atlas, index % cellsAcross, index / cellsAcross,
            cellSize, x, y, tint);
    }

    private static void AddAtlasQuad(
        QuadBatcher batcher,
        GlyphAtlas atlas,
        int col,
        int row,
        int cellSize,
        int x,
        int y,
        Color4 tint)
    {
        var insetX = 0.5f / atlas.Width;
        var insetY = 0.5f / atlas.Height;

        var uMin = new Vector2(col * cellSize / (float)atlas.Width + insetX,
                               row * cellSize / (float)atlas.Height + insetY);
        var uMax = new Vector2((col + 1) * cellSize / (float)atlas.Width - insetX,
                               (row + 1) * cellSize / (float)atlas.Height - insetY);

        batcher.AddTexturedQuad(x, y, 1, 1, tint, uMin, uMax);
    }
}
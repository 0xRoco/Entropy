using Entropy.Engine.ECS.Components;
using OpenTK.Mathematics;

namespace Entropy.Engine.World;

public class TileMap(int width, int height)
{
    private readonly Tile[] _tiles = new Tile[width * height];

    public int Width => width;
    public int Height => height;

    public Tile this[int x, int y]
    {
        get
        {
            if ((uint)x >= width || (uint)y >= height)
                throw new ArgumentOutOfRangeException($"Coordinates ({x}, {y}) are out of bounds for tile map of size {width}x{height}.");
            return _tiles[y * width + x];
        }
        private set => _tiles[y * width + x] = value;
    }

    public void SetTile(int x, int y, Tile tile)
    {
        if ((uint)x >= width || (uint)y >= height)
            throw new ArgumentOutOfRangeException($"Coordinates ({x}, {y}) are out of bounds for tile map of size {width}x{height}.");
        _tiles[y * width + x] = tile;
    }
}
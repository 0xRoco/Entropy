using OpenTK.Mathematics;

namespace Entropy.Engine.World;

public static class Fov
{
    public static void Compute(Vector2i origin, int radius, TileMap map, VisibilityMap visibility)
    {
        visibility.ClearVisible();

        if (origin.X >= 0 && origin.X < map.Width && origin.Y >= 0 && origin.Y < map.Height)
            visibility.SetVisible(origin.X, origin.Y);

        var min = new Vector2i(origin.X - radius, origin.Y - radius);
        var max = new Vector2i(origin.X + radius, origin.Y + radius);

        for (var x = min.X; x <= max.X; x++)
        {
            CastRay(origin, new Vector2i(x, min.Y), radius, map, visibility);
            CastRay(origin, new Vector2i(x, max.Y), radius, map, visibility);
        }
        for (var y = min.Y + 1; y < max.Y; y++)
        {
            CastRay(origin, new Vector2i(min.X, y), radius, map, visibility);
            CastRay(origin, new Vector2i(max.X, y), radius, map, visibility);
        }
        //Console.WriteLine($"FOV computed from origin ({origin.X}, {origin.Y}) with radius {radius}. Visible tiles: {visibility.VisibleCount()}");
    }
    
    private static void CastRay(Vector2i from, Vector2i to, int radius, TileMap map, VisibilityMap visibility)
    {
        int x = from.X, y = from.Y;
        var dx = Math.Abs(to.X - from.X);
        var dy = Math.Abs(to.Y - from.Y);
        var sx = from.X < to.X ? 1 : -1;
        var sy = from.Y < to.Y ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            if (x < 0 || x >= map.Width || y < 0 || y >= map.Height) return;

            var distX = x - from.X;
            var distY = y - from.Y;
            if (distX * distX + distY * distY > radius * radius) return;

            visibility.SetVisible(x, y);

            if (map[x, y].Opaque && (x != from.X || y != from.Y)) return;

            if (x == to.X && y == to.Y) return;

            var e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x += sx; }
            if (e2 < dx)  { err += dx; y += sy; }
        }
    }


}
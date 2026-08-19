using Entropy.Engine.Core;
using OpenTK.Mathematics;

namespace Entropy.Engine.World;

public static class MapGenerator
{
    public static (TileMap map, List<Vector2i> roomCenters) Generate(int width, int height, Rng rng,
        int roomAttempts = 20, int roomMinSize = 4, int roomMaxSize = 10)
    {
        var map = new TileMap(width, height);
        FillWalls(map);
        var rooms = new List<Room>();
        for (var i = 0; i < roomAttempts; i++)
        {
            var w = rng.Next(roomMinSize, roomMaxSize + 1);
            var h = rng.Next(roomMinSize, roomMaxSize + 1);
            var x = rng.Next(1, width - w - 1);
            var y = rng.Next(1, height - h - 1);
            
            var room = new Room(x, y, w, h);
            if (rooms.Exists(r => r.Intersects(room, 1))) continue;
            
            CarveRoom(map, room);
            if (rooms.Count > 0)
                CarveCorridor(map, rooms[^1], room, rng);
            rooms.Add(room);
        }

        return (map, rooms.Select(r => new Vector2i(r.CenterX, r.CenterY)).ToList());
    }

    private static void FillWalls(TileMap map)
    {
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                map.SetTile(x, y, Tile.Wall);
            }
        }
    }

    private static void CarveRoom(TileMap map, Room room)
    {
        for (var y = room.Y; y < room.Y + room.Height; y++)
        {
            for (var x = room.X; x < room.X + room.Width; x++)
            {
                map.SetTile(x, y, Tile.Floor);
            }
        }
    }

    private static void CarveCorridor(TileMap map, Room from, Room to, Rng rng)
    {
        var x1 = from.CenterX;
        var y1 = from.CenterY;
        var x2 = to.CenterX;
        var y2 = to.CenterY;

        if (rng.Chance(0.5f))
        {
            CarveHorizontal(map, x1, x2, y1);
            CarveVertical(map, y1, y2, x2);
        }
        else
        {
            CarveVertical(map, y1, y2, x1);
            CarveHorizontal(map, x1, x2, y2);
        }
    }
    
    private static void CarveHorizontal(TileMap map, int x1, int x2, int y)
    {
        for (var x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
            map.SetTile(x, y, Tile.Floor);
    }

    private static void CarveVertical(TileMap map, int y1, int y2, int x)
    {
        for (var y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
            map.SetTile(x, y, Tile.Floor);
    }
    
    private readonly record struct Room(int X, int Y, int Width, int Height)
    {
        public int CenterX => X + Width / 2;
        public int CenterY => Y + Height / 2;
        
        public bool Intersects(Room other, int padding) =>
            X - padding < other.X + other.Width &&
            X + Width + padding > other.X &&
            Y - padding < other.Y + other.Height &&
            Y + Height + padding > other.Y;
    }

}
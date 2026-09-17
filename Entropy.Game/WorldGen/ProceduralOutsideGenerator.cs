using Entropy.Content;
using Entropy.Engine.Core;
using Entropy.Engine.World;
using Entropy.Simulation;
using OpenTK.Mathematics;

namespace Entropy.Game.WorldGen;

public sealed record GeneratedBuilding(string TemplateId, Vector2i Origin);

public sealed record GeneratedOutsideMap(
    TileMap Map,
    IReadOnlyList<GeneratedBuilding> Buildings);

public static class ProceduralOutsideGenerator
{
    public static GeneratedOutsideMap Generate(
        DefinitionRegistry definitions,
        Rng rng,
        int width = 96,
        int height = 64)
    {
        if (width < 24 || height < 24)
            throw new ArgumentOutOfRangeException(nameof(width), "Outside maps must be at least 24x24.");

        var map = new TileMap(width, height);
        var grass = definitions.TileOf("grass");
        var road = definitions.TileOf("road_asphalt");
        var sidewalk = definitions.TileOf("sidewalk");
        var wall = definitions.TileOf("wall_brick");
        Fill(map, grass);

        for (var y = 10; y < height; y += 20)
        {
            FillRect(map, 0, y, width, 4, road);
            FillRect(map, 0, y - 2, width, 2, sidewalk);
            FillRect(map, 0, y + 4, width, 2, sidewalk);
        }

        for (var x = 14; x < width; x += 24)
        {
            FillRect(map, x, 0, 4, height, road);
            FillRect(map, x - 2, 0, 2, height, sidewalk);
            FillRect(map, x + 4, 0, 2, height, sidewalk);
        }

        for (var y = 10; y < height; y += 20)
        for (var x = 14; x < width; x += 24)
            FillRect(map, x - 2, y, 8, 4, road);

        var templates = definitions.BuildingTemplates.OrderBy(template => template.Id).ToList();
        var buildings = new List<GeneratedBuilding>();
        if (templates.Count == 0)
            return new GeneratedOutsideMap(map, buildings);

        for (var y = 2; y < height - 12; y += 20)
        for (var x = 2; x < width - 12; x += 24)
        {
            var template = templates[rng.Next(templates.Count)];
            if (template.Width + 1 >= width || template.Height + 1 >= height)
                continue;
            var origin = new Vector2i(
                Math.Min(x + rng.Next(0, 3), width - template.Width - 1),
                Math.Min(y + rng.Next(0, 3), height - template.Height - 1));
            if (OverlapsRoad(origin, template.Width, template.Height, width, height))
                continue;

            FillRect(map, origin.X, origin.Y, template.Width, template.Height, wall);
            buildings.Add(new GeneratedBuilding(template.Id, origin));
        }

        return new GeneratedOutsideMap(map, buildings);
    }

    private static bool OverlapsRoad(Vector2i origin, int width, int height, int mapWidth, int mapHeight)
    {
        for (var y = origin.Y; y < origin.Y + height; y++)
        for (var x = origin.X; x < origin.X + width; x++)
            if ((y >= 8 && y < 16) || (y >= 28 && y < 36) || (y >= 48 && y < 56) ||
                (x >= 12 && x < 20) || (x >= 36 && x < 44) || (x >= 60 && x < 68) || (x >= 84 && x < 92))
                return true;

        return origin.X < 0 || origin.Y < 0 || origin.X + width > mapWidth || origin.Y + height > mapHeight;
    }

    private static void Fill(TileMap map, Tile tile)
    {
        for (var y = 0; y < map.Height; y++)
        for (var x = 0; x < map.Width; x++)
            map.SetTile(x, y, tile);
    }

    private static void FillRect(TileMap map, int x, int y, int width, int height, Tile tile)
    {
        for (var row = y; row < y + height; row++)
        for (var column = x; column < x + width; column++)
            map.SetTile(column, row, tile);
    }
}

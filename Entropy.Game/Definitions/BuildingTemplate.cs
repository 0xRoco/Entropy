using Entropy.Engine.World;
using OpenTK.Mathematics;

namespace Entropy.Game.Definitions;

public class BuildingTemplate
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required List<string> Grid { get; init; }
    public required Dictionary<char, string> Legend { get; init; }
    public required Dictionary<string, Vector2i> Anchors { get; init; }

    public int Width => Grid[0].Length;
    public int Height => Grid.Count;

    public TileMap BuildMap(DefinitionRegistry defs)
    {
        var map = new TileMap(Width, Height);

        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
        {
            var marker = Grid[y][x];
            map.SetTile(x, y, defs.TileOf(Legend[marker]));
        }

        return map;
    }

    public Vector2i Anchor(string name) =>
        Anchors.TryGetValue(name, out var tile)
            ? tile
            : throw new KeyNotFoundException(
                $"Building template '{Id}' has no anchor named '{name}'.");
}
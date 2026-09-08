using OpenTK.Mathematics;

namespace Entropy.Content;

public class BuildingTemplate
{
    public string? Comment { get; set; }
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required List<string> Grid { get; set; }
    public required Dictionary<char, string> Legend { get; set; }
    public required Dictionary<string, Vector2i> Anchors { get; set; }

    public int Width => Grid[0].Length;
    public int Height => Grid.Count;

    public bool HasAnchor(string name) => Anchors.ContainsKey(name);

    public Vector2i Anchor(string name) =>
        Anchors.TryGetValue(name, out var tile)
            ? tile
            : throw new KeyNotFoundException(
                $"Building template '{Id}' has no anchor named '{name}'.");
}

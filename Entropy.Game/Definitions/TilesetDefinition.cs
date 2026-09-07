using OpenTK.Mathematics;

namespace Entropy.Game.Definitions;

public class TilesetDefinition
{
    public required string Id { get; init; }
    public string Mode { get; init; } = "ascii";
    public string Atlas { get; init; } = "";
    public int CellSize { get; init; } = 16;

    public Dictionary<string, Vector2i> Sprites { get; init; } = new();
}

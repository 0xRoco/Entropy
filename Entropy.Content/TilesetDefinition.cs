using OpenTK.Mathematics;

namespace Entropy.Content;

public class TilesetDefinition
{
    public string? Comment { get; set; }
    public required string Id { get; set; }
    public string Mode { get; set; } = "ascii";
    public string Atlas { get; set; } = "";
    public int CellSize { get; set; } = 16;
    
    public Dictionary<string, Vector2i> Sprites { get; set; } = new();
}

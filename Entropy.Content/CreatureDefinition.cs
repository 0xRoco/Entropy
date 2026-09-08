using OpenTK.Mathematics;

namespace Entropy.Content;

public class CreatureDefinition
{
    public string? Comment { get; set; }
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required char Symbol { get; set; }
    public required Color4 Color { get; set; }
    public int Health { get; set; } = 5;
    public int Speed { get; set; } = 100;
    public int SightRadius { get; set; } = 5;
    public int SmellRadius { get; set; }
    public string Behavior { get; set; } = "wander";
    public bool Hostile { get; set; }
}

using OpenTK.Mathematics;

namespace Entropy.Game.Definitions;

public class CreatureDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required char Symbol { get; init; }
    public required Color4 Color { get; init; }
    public int Health { get; init; }
    public int Speed { get; init; } = 100;
    public int SightRadius { get; init; }
    public int SmellRadius { get; init; }
    public string Behavior { get; init; } = "wander";
    public bool Hostile { get; init; }

}
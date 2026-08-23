namespace Entropy.Game.Definitions;

public abstract record ItemEffect
{
    public sealed record Heal(int Amount) : ItemEffect;
}
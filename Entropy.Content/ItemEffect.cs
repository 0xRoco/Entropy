namespace Entropy.Content;

public abstract record ItemEffect
{
    public sealed record Heal(int Amount) : ItemEffect;
    public sealed record Damage(int Amount) : ItemEffect;
    public sealed record Nourish(int Amount) : ItemEffect;
    public sealed record Hydrate(int Amount) : ItemEffect;
}

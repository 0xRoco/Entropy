namespace Entropy.Engine.Core;

public sealed class Rng(int seed)
{
    public int Seed { get; } = seed;
    public int Next(int max) => _random.Next(max);
    public int Next (int min, int max) => _random.Next(min, max);
    public float NextFloat() => _random.NextSingle();
    public bool Chance (float probability) => _random.NextSingle() < probability;
    public T Pick<T>(IList<T> list) => list[_random.Next(list.Count)];
    
    private readonly Random _random = new(seed);
}
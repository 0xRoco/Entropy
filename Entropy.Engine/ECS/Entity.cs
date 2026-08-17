namespace Entropy.Engine.ECS;

public readonly struct Entity(int id) : IEquatable<Entity>
{
    public readonly int Id = id;

    public override string ToString()
    {
        return $"Entity({Id})";
    }
    
    public bool Equals(Entity other) => Id == other.Id;

    public override bool Equals(object? obj)
    {
        return obj is Entity other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }
}
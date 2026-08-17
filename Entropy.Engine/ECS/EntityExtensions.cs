namespace Entropy.Engine.ECS;

public static class EntityExtensions
{
    public static Entity With<T>(this Entity e, World world, T component)
    {
        world.Set(e, component);
        return e;
    }
}
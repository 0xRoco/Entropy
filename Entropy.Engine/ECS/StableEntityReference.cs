namespace Entropy.Engine.ECS;

public readonly record struct StableEntityReference(long Id)
{
    public bool IsValid => Id > 0;

    public Entity Resolve(World world) => world.ResolveStableId(Id);

    public static StableEntityReference From(World world, Entity entity) =>
        new(world.StableId(entity));
}

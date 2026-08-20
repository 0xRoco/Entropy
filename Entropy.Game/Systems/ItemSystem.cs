using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public static class ItemSystem
{
    public static List<Entity> ItemsAt(World world, Vector2 tile)
    {
        var result = new List<Entity>();
        foreach (var entity in world.Query<Position, Item>())
        {
            var p = world.Get<Position>(entity).Value;
            if ((int)p.X == (int)tile.X && (int)p.Y == (int)tile.Y)
                result.Add(entity);
        }
        
        return result;
    }

    public static bool TryPickup(World world, Entity picker, Entity item)
    {
        if (!world.IsAlive(item) || !world.Has<Item>(item)) return false;
        if (world.Has<InContainer>(item)) return false;
        
        if (!world.Has<Container>(picker)) world.Set(picker, Container.Create());
        ref var container = ref world.Get<Container>(picker);
        
        world.Set(item, new InContainer {Parent = picker});
        world.Remove<Position>(item);
        container.Items.Add(item);
        return true;
    }

    public static void Drop(World world, Entity item, int x, int y)
    {
        var parent = world.Get<InContainer>(item).Parent;
        if (world.IsAlive(parent) && world.Has<Container>(parent))
            world.Get<Container>(parent).Items.Remove(item);
        
        world.Remove<InContainer>(item);
        world.Set(item, new Position {Value = new Vector2(x, y)});
    }

    public static List<Entity> GetItems(World world, Entity container)
    {
        return world.Has<Container>(container)
            ? world.Get<Container>(container).Items
            : [];
    }
}
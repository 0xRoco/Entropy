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
        AddToContainer(world, picker, item);
        return true;
    }

    public static void Drop(World world, Entity item, int x, int y)
    {
        RemoveFromContainer(world, item);
        world.Set(item, new Position { Value = new Vector2(x, y) });
    }

    public static List<Entity> GetItems(World world, Entity container)
    {
        return world.Has<Container>(container)
            ? world.Get<Container>(container).Items
            : [];
    }
    
    public static void RemoveFromContainer(World world, Entity item)
    {
        if (!world.Has<InContainer>(item)) return;
        var parent = world.Get<InContainer>(item).Parent;
        if (world.IsAlive(parent) && world.Has<Container>(parent))
            world.Get<Container>(parent).Items.Remove(item);
        world.Remove<InContainer>(item);
    }

    private static void AddToContainer(World world, Entity picker, Entity item)
    {
        var container = world.Get<Container>(picker);

        if (!world.Has<Stackable>(item))
        {
            container.Items.Add(item);
            return;
        }
        
        ref var incoming = ref world.Get<Stackable>(item);
        foreach (var existing in container.Items)
        {
            if (!world.IsAlive(existing) || !world.Has<Stackable>(existing)) continue;
            if (world.Get<ItemIdentity>(existing).DefinitionId != world.Get<ItemIdentity>(item).DefinitionId) continue;
            
            ref var stack = ref world.Get<Stackable>(existing);
            var space = stack.MaxStack - stack.Count;
            var moved = Math.Min(space, incoming.Count);
            stack.Count += moved;
            incoming.Count -= moved;

            if (incoming.Count > 0) continue;
            
            world.Destroy(item);
            return;
        }
        
        container.Items.Add(item);
    }
}
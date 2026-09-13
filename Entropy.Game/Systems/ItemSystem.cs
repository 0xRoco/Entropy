using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public static class ItemSystem
{
    public static List<Entity> ItemsAt(World world, string mapId, Vector2 tile)
    {
        var result = new List<Entity>();

        foreach (var entity in world.Query<Position, Item>())
        {
            if (!world.Has<Location>(entity) ||
                world.Get<Location>(entity).MapId != mapId)
            {
                continue;
            }

            var pos = world.Get<Position>(entity).Value;

            if ((int)pos.X == (int)tile.X &&
                (int)pos.Y == (int)tile.Y)
            {
                result.Add(entity);
            }
        }

        return result;
    }

    public static bool TryPickup(World world, Entity picker, Entity item)
    {
        if (!world.IsAlive(item) || !world.Has<Item>(item)) return false;
        if (world.Has<InContainer>(item)) return false;

        return Transfer(world, item, picker);
    }

    public static bool Transfer(World world, Entity item, Entity destination)
    {
        if (!world.IsAlive(item) || !world.IsAlive(destination)) return false;
        if (item.Equals(destination) || WouldCreateCycle(world, item, destination)) return false;

        if (world.Has<InContainer>(item) &&
            world.Get<InContainer>(item).Parent.Equals(destination))
            return true;

        if (!world.Has<Container>(destination))
            world.Set(destination, Container.Create());

        ref var destinationContainer = ref world.Get<Container>(destination);
        var alreadyContained = destinationContainer.Items.Contains(item);
        if (!alreadyContained && destinationContainer.Items.Count >= destinationContainer.Slots)
            return false;

        RemoveFromContainer(world, item);
        world.Set(item, new InContainer { Parent = destination });
        world.Remove<Position>(item);
        AddToContainer(world, destination, item);
        return true;
    }

    public static void Drop(World world, string mapId, Entity item, int x, int y)
    {
        if (!world.IsAlive(item)) return;
        RemoveFromContainer(world, item);
        world.Set(item, new Position { Value = new Vector2(x, y) });
        world.Set(item, new Location { MapId = mapId });
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

    private static bool WouldCreateCycle(World world, Entity item, Entity destination)
    {
        var current = destination;
        var visited = new HashSet<Entity>();
        while (world.IsAlive(current) && world.Has<InContainer>(current))
        {
            if (!visited.Add(current)) return true;
            current = world.Get<InContainer>(current).Parent;
            if (current.Equals(item)) return true;
        }

        return false;
    }
}

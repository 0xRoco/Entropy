using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Behaviors;
using Entropy.Game.Components;
using Entropy.Game.Systems;
using Entropy.Content;
using Entropy.Game.Components.AI;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.ItemEffects;
using Entropy.Game.Components.Simulation;
using Entropy.Game.Components.Spatial;
using Entropy.Game.Components.Tags;
using Entropy.Game.Components.Vitals;
using OpenTK.Mathematics;

namespace Entropy.Game;

public static class EntitySpawner
{
    public static Entity CreatePlayer(World world, string mapId, CreatureDefinition def, int x, int y)
    {
        var e = world.Create()
            .With(world, new Position { Value = new Vector2(x, y) })
            .With(world, new Location { MapId = mapId })
            .With(world, new Glyph { Character = def.Symbol, Foreground = def.Color })
            .With(world, new PlayerControlled())
            .With(world, new Health { Current = def.Health, Max = def.Health })
            .With(world, new Actor())
            .With(world, new Speed { Value = def.Speed })
            .With(world, new CreatureIdentity { DefinitionId = def.Id })
            .With(world, new Facing { Direction = new Vector2i(1, 0) })
            .With(world, new Hunger { Current = 480, Max = 480 })
            .With(world, new Thirst { Current = 240, Max = 240 })
            .With(world, new Fatigue { Current = 960, Max = 960 });

        return e;
    }

    public static Entity CreateHuman(World world, string mapId, CreatureDefinition def, int x, int y, string name)
    {
        var e = BuildCreature(world, mapId, def, x, y);
        e.With(world, new Named { Name = name });
        e.With(world, new CreatureIdentity { DefinitionId = def.Id });
        return e;
    }

    public static Entity CreateCop(World world, string mapId, CreatureDefinition def, int x, int y, Entity target)
    {
        var e = world.Create()
            .With(world, new Position { Value = new Vector2(x, y) })
            .With(world, new Location { MapId = mapId })
            .With(world, new Glyph { Character = def.Symbol, Foreground = def.Color })
            .With(world, new Named { Name = "Officer" })
            .With(world, new Health { Current = def.Health, Max = def.Health })
            .With(world, new Actor())
            .With(world, new Speed { Value = def.Speed })
            .With(world, new CreatureIdentity { DefinitionId = def.Id })
            .With(world, new Perception { SightRadius = def.SightRadius, SmellRadius = def.SmellRadius })
            .With(world, Awareness.Create())
            .With(world, WitnessMemory.Create())
            .With(world, new AIState { Mode = AIMode.Hunt, Target = target })
            .With(world, new Behavior { BehaviorId = "respond" });
        return e;
    }

    public static Entity CreateItem(World world, string mapId, ItemDefinition def, int x, int y, int count = 1)
    {
        var e = world.Create()
            .With(world, new Position { Value = new Vector2(x, y) })
            .With(world, new Location { MapId = mapId })
            .With(world, new Glyph { Character = def.Symbol, Foreground = def.Color, RememberedInFog = true })
            .With(world, new Item())
            .With(world, new ItemIdentity { Name = def.Name, DefinitionId = def.Id });

        if (def.Stackable)
            e.With(world, new Stackable { Count = count, MaxStack = def.MaxStack });
        if (def.Effect<ItemEffect.Heal>() is { } heal)
            e.With(world, new Healing { Amount = heal.Amount });
        if (def.Effect<ItemEffect.Damage>() is { } damage)
            e.With(world, new Damage { Amount = damage.Amount });
        if (def.Effect<ItemEffect.Nourish>() is { } nourish)
            e.With(world, new Nutrition { Amount = nourish.Amount });
        if (def.Effect<ItemEffect.Hydrate>() is { } hydrate)
            e.With(world, new Hydration { Amount = hydrate.Amount });

        return e;
    }
    
    public static Entity CreateWorldObject(World world, string mapId, WorldObjectDefinition def, int x, int y)
    {
        var e = world.Create()
            .With(world, new Position { Value = new Vector2(x, y) })
            .With(world, new Location { MapId = mapId })
            .With(world, new Glyph { Character = def.Symbol, Foreground = def.Color, RememberedInFog = true })
            .With(world, new Named { Name = def.Name })
            .With(world, new WorldObjectIdentity { Name = def.Name, DefinitionId = def.Id })
            .With(world, new Solid { Blocks = !def.HasFlag("walkable") });

        if (def.IsContainer)
            e.With(world, Container.WithSlots(def.ContainerSlots));

        return e;
    }

    public static Entity SpawnIntoContainer(World world, Entity container, ItemDefinition def, int count = 1)
    {
        var location = world.Get<Location>(container).MapId;
        var position = world.Get<Position>(container).Value;
        var item = CreateItem(world, location, def, (int)position.X, (int)position.Y, count);
        ItemSystem.Transfer(world, item, container);
        return item;
    }

    private static Entity BuildCreature(World world, string mapId, CreatureDefinition def, int x, int y)
    {
        var e = world.Create()
            .With(world, new Position { Value = new Vector2(x, y) })
            .With(world, new Location { MapId = mapId })
            .With(world, new Glyph { Character = def.Symbol, Foreground = def.Color })
            .With(world, new Health { Current = def.Health, Max = def.Health })
            .With(world, new Actor())
            .With(world, new Speed { Value = def.Speed })
            .With(world, new Perception { SightRadius = def.SightRadius, SmellRadius = def.SmellRadius })
            .With(world, Awareness.Create())
            .With(world, WitnessMemory.Create())
            .With(world, new AIState { Mode = AIMode.Idle })
            .With(world, new Behavior { BehaviorId = def.Behavior });

        if (def.Hostile)
            e.With(world, new Hostile());

        return e;
    }


}

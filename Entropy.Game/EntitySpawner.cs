using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Behaviors;
using Entropy.Game.Components;
using Entropy.Game.Definitions;
using OpenTK.Mathematics;

namespace Entropy.Game;

public static class EntitySpawner
{
    public static Entity CreatePlayer(World world, int x, int y)
    {
        var e = world.Create()
            .With(world, new Position { Value = new Vector2(x, y) })
            .With(world, new Glyph { Character = '@', Foreground = Color4.White })
            .With(world, new PlayerControlled())
            .With(world, new Health { Current = 10, Max = 10 })
            .With(world, new Actor())
            .With(world, new Speed { Value = 100 })
            .With(world, new Hunger { Current = 480, Max = 480 })
            .With(world, new Thirst { Current = 240, Max = 240 })
            .With(world, new Fatigue { Current = 960, Max = 960 });
        
        Console.WriteLine($"Created player entity {e.Id} at position ({x}, {y})");
        return e;
    }
    
    public static Entity CreateHuman(World world, CreatureDefinition def, int x, int y, string name)
    {
        var e = BuildCreature(world, def, x, y);
        e.With(world, new Named { Name = name });
        return e;
    }

    public static Entity CreateCop(World world, CreatureDefinition def, int x, int y, Entity target)
    {
        var e = world.Create()
            .With(world, new Position { Value = new Vector2(x, y) })
            .With(world, new Glyph { Character = def.Symbol, Foreground = def.Color })
            .With(world, new Named { Name = "Officer" })
            .With(world, new Health { Current = def.Health, Max = def.Health })
            .With(world, new Actor())
            .With(world, new Speed { Value = def.Speed })
            .With(world, new Perception { SightRadius = def.SightRadius, SmellRadius = def.SmellRadius })
            .With(world, Awareness.Create())
            .With(world, WitnessMemory.Create())
            .With(world, new Behavior { Impl = new RespondBehavior(target) });
        return e;
    }
    
    public static Entity CreateZombie(World world, int x, int y)
    {
        var e = world.Create()
            .With(world, new Position { Value = new Vector2(x, y) })
            .With(world, new Glyph { Character = 'z', Foreground = Color4.Green })
            .With(world, new Hostile())
            .With(world, new Health { Current = 3, Max = 3 })
            .With(world, new Actor())
            .With(world, new Behavior { Impl = new ZombieBehavior() })
            .With(world, new Perception { SightRadius = 7, SmellRadius = 3 })
            .With(world, Awareness.Create())
            .With(world, new AIState { Mode = AIMode.Idle })
            .With(world, new Speed {Value = 100});
        
        Console.WriteLine($"Created zombie entity {e.Id} at position ({x}, {y})");
        return e;
    }
    
    public static Entity CreateItem(World world, ItemDefinition def, int x, int y, int count = 1)
    {
        var e = world.Create()
            .With(world, new Position { Value = new Vector2(x, y) })
            .With(world, new Glyph { Character = def.Symbol, Foreground = def.Color })
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
    
    private static Entity BuildCreature(World world, CreatureDefinition def, int x, int y)
    {
        var e = world.Create()
            .With(world, new Position { Value = new Vector2(x, y) })
            .With(world, new Glyph { Character = def.Symbol, Foreground = def.Color })
            .With(world, new Health { Current = def.Health, Max = def.Health })
            .With(world, new Actor())
            .With(world, new Speed { Value = def.Speed })
            .With(world, new Perception { SightRadius = def.SightRadius, SmellRadius = def.SmellRadius })
            .With(world, Awareness.Create())
            .With(world, WitnessMemory.Create())
            .With(world, new AIState { Mode = AIMode.Idle })
            .With(world, new Behavior { Impl = BehaviorCatalog.Get(def.Behavior) });

        if (def.Hostile)
            e.With(world, new Hostile());

        return e;
    }


}
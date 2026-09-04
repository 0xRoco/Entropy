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
    
    public static Entity CreateHuman(World world, int x, int y, string name)
    {
        var e = world.Create()
            .With(world, new Position { Value = new Vector2(x, y) })
            .With(world, new Glyph { Character = 'h', Foreground = Color4.Blue })
            .With(world, new Named { Name = name })
            .With(world, new Health { Current = 5, Max = 5 })
            .With(world, new Actor())
            .With(world, new Speed { Value = 100 })
            .With(world, new Perception { SightRadius = 5, SmellRadius = 2 })
            .With(world, Awareness.Create())
            .With(world, WitnessMemory.Create())
            .With(world, new AIState { Mode = AIMode.Idle })
            .With(world, new Behavior { Impl = new WanderBehavior() });
        
        Console.WriteLine($"Created human entity {e.Id} at position ({x}, {y})");
        return e;
    }
    
    public static Entity CreateCop(World world, int x, int y, Vector2i scene, Entity target)
    {
        var e = world.Create()
            .With(world, new Position { Value = new Vector2(x, y) })
            .With(world, new Glyph { Character = 'P', Foreground = Color4.LightBlue })
            .With(world, new Named { Name = "Officer" })
            .With(world, new Health { Current = 8, Max = 8 })
            .With(world, new Actor())
            .With(world, new Speed { Value = 100 })
            .With(world, Awareness.Create())
            .With(world, new Perception { SightRadius = 6, SmellRadius = 0 })
            .With(world, WitnessMemory.Create())
            .With(world, new AIState { Mode = AIMode.Idle })
            .With(world, new Behavior { Impl = new RespondBehavior(target) });
        
        Console.WriteLine($"Created cop entity {e.Id} at position ({x}, {y})");
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
            e.With(world, new Nutrition() { Amount = nourish.Amount });
        if (def.Effect<ItemEffect.Hydrate>() is { } hydrate)
            e.With(world, new Hydration() { Amount = hydrate.Amount });

        return e;
    }

}
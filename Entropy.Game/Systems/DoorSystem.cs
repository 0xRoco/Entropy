using Entropy.Engine.ECS;
using Entropy.Engine.World;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.Spatial;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public static class DoorSystem
{
    public static DoorKey KeyFor(MapTransition transition) => new(
        transition.FromMap,
        transition.FromTile,
        transition.ToMap,
        transition.ToTile);

    public static bool TryGet(GameContext context, MapTransition transition,
        out DoorDefinition definition, out DoorState state)
    {
        var key = KeyFor(transition);
        if (context.DoorDefinitions.TryGetValue(key, out definition!) &&
            context.DoorStates.TryGetValue(key, out state))
            return true;

        definition = null!;
        state = default;
        return false;
    }

    public static bool IsPassable(GameContext context, MapTransition transition) =>
        !TryGet(context, transition, out _, out var state) || !state.Locked || state.Broken;

    public static ActionResult Unlock(GameContext context, Entity player, MapTransition transition)
    {
        if (!TryGet(context, transition, out var definition, out var state) ||
            !state.Locked || state.Broken || !HasItemWithFlag(context, player, definition.RequiredKeyFlag))
            return ActionResult.Failed;

        state.Locked = false;
        context.DoorStates[KeyFor(transition)] = state;
        context.Log.Add("You unlock the door quietly.", Color4.Cyan);
        return ActionResult.Turn;
    }

    public static ActionResult ForceEntry(GameContext context, Entity player,
        MapTransition transition, Vector2i source)
    {
        return ForceEntry(context, player, transition, source,
            TryGet(context, transition, out var definition, out _) ? definition.RequiredToolFlag : string.Empty,
            "smash", 12);
    }

    public static ActionResult ForceEntry(GameContext context, Entity player,
        MapTransition transition, Vector2i source, string toolFlag, string method, int noiseRadius)
    {
        if (!TryGet(context, transition, out var definition, out var state) ||
            !state.Locked || state.Broken || !HasItemWithFlag(context, player, toolFlag))
            return ActionResult.Failed;

        state.Locked = false;
        state.Broken = true;
        context.DoorStates[KeyFor(transition)] = state;
        context.Log.Add($"You {method} the lock. The noise carries down the street.", Color4.OrangeRed);
        NoiseSystem.Emit(context, source, noiseRadius, $"Something is {method}ing into the door");
        return ActionResult.Turn;
    }

    public static ActionResult UnlockContainer(GameContext context, Entity player, Entity container)
    {
        if (!TryGetLock(context, container, out var state) || !state.Locked || state.Broken ||
            !HasItemWithFlag(context, player, state.RequiredKeyFlag))
            return ActionResult.Failed;

        state.Locked = false;
        context.World.Set(container, state);
        context.Log.Add("You unlock it quietly.", Color4.Cyan);
        return ActionResult.Turn;
    }

    public static ActionResult ForceContainerEntry(GameContext context, Entity player, Entity container,
        Vector2i source, string toolFlag, string method, int noiseRadius)
    {
        if (!TryGetLock(context, container, out var state) || !state.Locked || state.Broken ||
            !HasItemWithFlag(context, player, toolFlag))
            return ActionResult.Failed;

        state.Locked = false;
        state.Broken = true;
        context.World.Set(container, state);
        context.Log.Add($"You {method} the lock. The noise carries down the street.", Color4.OrangeRed);
        NoiseSystem.Emit(context, source, noiseRadius, $"Something is {method}ing into the container");
        return ActionResult.Turn;
    }

    public static bool TryGetLock(GameContext context, Entity entity, out LockState state)
    {
        if (context.World.Has<LockState>(entity))
        {
            state = context.World.Get<LockState>(entity);
            return true;
        }

        state = default;
        return false;
    }

    public static bool HasItemWithFlag(GameContext context, Entity player, string flag)
    {
        var world = context.World;
        if (string.IsNullOrWhiteSpace(flag) || !world.Has<Container>(player))
            return false;

        return world.Get<Container>(player).Items
            .Where(world.IsAlive)
            .Where(item => world.Has<ItemIdentity>(item))
            .Select(item => context.Definitions.Item(world.Get<ItemIdentity>(item).DefinitionId))
            .Any(item => item.Flags.Contains(flag));
    }
}

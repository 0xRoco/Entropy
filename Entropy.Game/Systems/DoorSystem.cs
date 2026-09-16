using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.Spatial;
using OpenTK.Mathematics;
using Entropy.Simulation;

namespace Entropy.Game.Systems;

public static class DoorSystem
{
    public static DoorKey KeyFor(MapTransition transition) => new(
        transition.FromMap,
        transition.FromTile,
        transition.ToMap,
        transition.ToTile);

    public static bool TryGet(IGameRuntimeContext context, MapTransition transition,
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

    public static bool IsPassable(IGameRuntimeContext context, MapTransition transition) =>
        !TryGet(context, transition, out _, out var state) || !state.Locked || state.Broken;

    public static ActionResult Unlock(IGameRuntimeContext context, Entity player, MapTransition transition)
    {
        if (!TryGet(context, transition, out var definition, out var state) ||
            !state.Locked || state.Broken || !HasItemWithFlag(context, player, definition.RequiredKeyFlag))
            return ActionResult.Failed;

        state.Locked = false;
        context.DoorStates[KeyFor(transition)] = state;
        context.Events.Publish(new SimEvent(
            "door.unlock",
            "A door was unlocked quietly.",
            transition.FromMap,
            transition.FromTile,
            context.Clock.MinuteOfDay,
            context.World.IsAlive(player) ? context.World.StableId(player) : null));
        return ActionResult.Turn;
    }

    public static ActionResult ForceEntry(IGameRuntimeContext context, Entity player,
        MapTransition transition, Vector2i source)
    {
        return ForceEntry(context, player, transition, source,
            TryGet(context, transition, out var definition, out _) ? definition.RequiredToolFlag : string.Empty,
            "smash", 12);
    }

    public static ActionResult ForceEntry(IGameRuntimeContext context, Entity player,
        MapTransition transition, Vector2i source, string toolFlag, string method, int noiseRadius)
    {
        if (!TryGet(context, transition, out var definition, out var state) ||
            !state.Locked || state.Broken || !HasItemWithFlag(context, player, toolFlag))
            return ActionResult.Failed;

        state.Locked = false;
        state.Broken = true;
        context.DoorStates[KeyFor(transition)] = state;
        context.Events.Publish(new SimEvent(
            "door.force",
            $"You {method} the lock. The noise carries down the street.",
            transition.FromMap,
            source,
            context.Clock.MinuteOfDay,
            context.World.IsAlive(player) ? context.World.StableId(player) : null));
        NoiseSystem.Emit(context, source, noiseRadius, $"Something is {method}ing into the door");
        return ActionResult.Turn;
    }

    public static ActionResult UnlockContainer(IGameRuntimeContext context, Entity player, Entity container)
    {
        if (!TryGetLock(context, container, out var state) || !state.Locked || state.Broken ||
            !HasItemWithFlag(context, player, state.RequiredKeyFlag))
            return ActionResult.Failed;

        state.Locked = false;
        context.World.Set(container, state);
        var location = context.World.Has<Position>(container)
            ? context.World.Get<Position>(container).Value
            : OpenTK.Mathematics.Vector2.Zero;
        context.Events.Publish(new SimEvent(
            "container.unlock",
            "A container was unlocked quietly.",
            context.World.Has<Location>(container) ? context.World.Get<Location>(container).MapId : context.MapId,
            new Vector2i((int)location.X, (int)location.Y),
            context.Clock.MinuteOfDay,
            context.World.IsAlive(player) ? context.World.StableId(player) : null,
            context.World.IsAlive(container) ? context.World.StableId(container) : null));
        return ActionResult.Turn;
    }

    public static ActionResult ForceContainerEntry(IGameRuntimeContext context, Entity player, Entity container,
        Vector2i source, string toolFlag, string method, int noiseRadius)
    {
        if (!TryGetLock(context, container, out var state) || !state.Locked || state.Broken ||
            !HasItemWithFlag(context, player, toolFlag))
            return ActionResult.Failed;

        state.Locked = false;
        state.Broken = true;
        context.World.Set(container, state);
        context.Events.Publish(new SimEvent(
            "container.force",
            $"You {method} the lock. The noise carries down the street.",
            context.World.Has<Location>(container) ? context.World.Get<Location>(container).MapId : context.MapId,
            source,
            context.Clock.MinuteOfDay,
            context.World.IsAlive(player) ? context.World.StableId(player) : null,
            context.World.IsAlive(container) ? context.World.StableId(container) : null));
        NoiseSystem.Emit(context, source, noiseRadius, $"Something is {method}ing into the container");
        return ActionResult.Turn;
    }

    public static bool TryGetLock(IGameRuntimeContext context, Entity entity, out LockState state)
    {
        if (context.World.Has<LockState>(entity))
        {
            state = context.World.Get<LockState>(entity);
            return true;
        }

        state = default;
        return false;
    }

    public static bool HasItemWithFlag(IGameRuntimeContext context, Entity player, string flag)
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

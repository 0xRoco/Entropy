using System.Drawing;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.Simulation;
using Entropy.Game.Components.Spatial;
using Entropy.Game.Components.Tags;
using OpenTK.Mathematics;
using Entropy.Simulation;

namespace Entropy.Game.Systems;

public static class SearchSystem
{
    public const int DurationMinutes = 5;

    public static ActionResult Start(IGameRuntimeContext context, Entity actor, Entity container)
    {
        var world = context.World;
        if (!world.IsAlive(container) || !world.Has<Container>(container) ||
            world.Has<Searched>(container) || ActivitySystem.IsActive(world, actor) ||
            (DoorSystem.TryGetLock(context, container, out var lockState) && lockState.Locked && !lockState.Broken))
            return ActionResult.Failed;

        var started = ActivitySystem.Start(context, actor, new Activity
        {
            Kind = ActivityKind.Search,
            RemainingMinutes = DurationMinutes,
            TotalMinutes = DurationMinutes,
            Target = StableEntityReference.From(world, container)
        });

        if (!started)
            return ActionResult.Failed;

        var name = world.Has<WorldObjectIdentity>(container)
            ? world.Get<WorldObjectIdentity>(container).Name
            : "container";
        context.Events.Publish(new SimEvent(
            "search.started",
            $"You begin searching the {name}.",
            context.MapId,
            new Vector2i((int)world.Get<Position>(actor).Value.X, (int)world.Get<Position>(actor).Value.Y),
            context.Clock.MinuteOfDay,
            world.IsAlive(actor) ? world.StableId(actor) : null,
            world.IsAlive(container) ? world.StableId(container) : null));
        return ActionResult.Turn;
    }

    public static void Complete(IGameRuntimeContext context, Entity actor, Entity container)
    {
        var world = context.World;
        if (!world.IsAlive(container) || !world.Has<Container>(container))
            return;

        world.Set(container, new Searched());
        var name = world.Has<WorldObjectIdentity>(container)
            ? world.Get<WorldObjectIdentity>(container).Name
            : "container";
        var count = world.Get<Container>(container).Items.Count(world.IsAlive);
        context.Events.Publish(new SimEvent(
            "search.completed",
            count <= 0
                ? $"You search the {name}. It is empty."
                : $"You search the {name}. You find something inside.",
            context.MapId,
            new Vector2i((int)world.Get<Position>(actor).Value.X, (int)world.Get<Position>(actor).Value.Y),
            context.Clock.MinuteOfDay,
            world.IsAlive(actor) ? world.StableId(actor) : null,
            world.IsAlive(container) ? world.StableId(container) : null));
    }
}

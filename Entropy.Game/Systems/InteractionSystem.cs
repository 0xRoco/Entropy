using Entropy.Content;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.UI;
using Entropy.Engine.World;
using Entropy.Game.Components;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.Spatial;
using Entropy.Game.Components.ItemEffects;
using Entropy.Game.Components.Tags;
using Entropy.Game.Components.Vitals;
using OpenTK.Mathematics;
using Entropy.Simulation;

namespace Entropy.Game.Systems;

public record WorldVerb(
    string Label,
    Func<IGameRuntimeContext, Entity, ActionResult> Execute,
    SimulationCommand? Command = null);

public static class InteractionSystem
{
    public static List<WorldVerb> GetVerbs(
        IGameRuntimeContext context, Entity player, Vector2i tile,
        Action<Entity> openContainer)
    {
        var verbs = new List<WorldVerb>();
        var world = context.World;
        var mapId = world.Get<Location>(player).MapId;
        var transition = context.Maps.TransitionAt(mapId, tile);

        if (transition is not null && IsAdjacent(world.Get<Position>(player).Value, tile) &&
            DoorSystem.TryGet(context, transition, out var door, out var doorState) &&
            doorState.Locked && !doorState.Broken)
        {
            if (!string.IsNullOrWhiteSpace(door.RequiredKeyFlag) && DoorSystem.HasItemWithFlag(context, player, door.RequiredKeyFlag))
            {
                verbs.Add(new WorldVerb("Unlock the door",
                    (ctx, p) => DoorSystem.Unlock(ctx, p, transition),
                    new UnlockDoorCommand(transition)));
            }

            AddDoorForceVerbs(context, verbs, player, transition, tile);

            verbs.Add(new WorldVerb("Examine the locked door",
                (ctx, p) =>
                {
                    Publish(ctx, p, "interaction.examine", "The door is locked. There may be another way in.");
                    return ActionResult.Free;
                }));
        }

        foreach (var entity in world.Query<Position, WorldObjectIdentity>())
        {
            if (!OccupiesTile(world, entity, mapId, tile)) continue;

            var identity = world.Get<WorldObjectIdentity>(entity);
            var def = context.Definitions.WorldObject(identity.DefinitionId);
            var adjacent = IsAdjacent(world.Get<Position>(player).Value, tile);
            if (world.Has<Container>(entity) && adjacent)
            {
                var target = entity;
                if (DoorSystem.TryGetLock(context, entity, out var lockState) &&
                    lockState.Locked && !lockState.Broken)
                {
                    if (DoorSystem.HasItemWithFlag(context, player, lockState.RequiredKeyFlag))
                        verbs.Add(new WorldVerb($"Unlock the {identity.Name}",
                            (ctx, p) => DoorSystem.UnlockContainer(ctx, p, target),
                            new UnlockContainerCommand(target)));

                    AddContainerForceVerbs(context, verbs, player, target, tile);
                    verbs.Add(new WorldVerb($"Examine the locked {identity.Name}",
                        (ctx, p) =>
                        {
                            Publish(ctx, p, "interaction.examine", "It is locked. There may be another way in.");
                            return ActionResult.Free;
                        }));
                }
                else if (!world.Has<Searched>(entity))
                {
                verbs.Add(new WorldVerb($"Search the {identity.Name}",
                        (ctx, p) => SearchSystem.Start(ctx, p, target),
                        new SearchCommand(target)));
                }
                else
                {
                    verbs.Add(new WorldVerb($"Open the {identity.Name}",
                        (ctx, _) =>
                        {
                             openContainer.Invoke(target);
                            return ActionResult.Turn;
                        }));
                }
            }

            if (adjacent && def.HasFlag("bed"))
            {
                var bed = entity;
                verbs.Add(new WorldVerb($"Sleep on the {identity.Name}",
                    (ctx, p) => SleepSystem.FallAsleep(ctx, p, bed),
                    new SleepCommand(bed)));
            }

                verbs.Add(new WorldVerb($"Examine the {identity.Name}",
                    (ctx, _) =>
                    {
                        ExamineWorldObject(ctx, entity);
                        return ActionResult.Free;
                    },
                    new ExamineEntityCommand(entity)));
        }

        foreach (var entity in world.Query<Position, Actor>())
        {
            if (entity.Equals(player)) continue;
            if (!OccupiesTile(world, entity, mapId, tile)) continue;

            var name = world.Has<Named>(entity)
                ? world.Get<Named>(entity).Name
                : "someone";
            var target = entity;

            if (IsAdjacent(world.Get<Position>(player).Value, tile))
                verbs.Add(new WorldVerb($"Attack {name}",
                    (ctx, _) => Attack(ctx, player, target),
                    new AttackCommand(target)));

                verbs.Add(new WorldVerb($"Talk to {name}",
                    (ctx, _) =>
                    {
                        Publish(ctx, player, "interaction.talk", $"{name} ignores you.");
                        return ActionResult.Free;
                    },
                    new TalkCommand(target)));
                verbs.Add(new WorldVerb($"Examine {name}",
                    (ctx, _) =>
                    {
                        ExamineNpc(ctx, target);
                        return ActionResult.Free;
                    },
                    new ExamineEntityCommand(target)));
        }

        foreach (var item in ItemSystem.ItemsAt(world, mapId, tile))
        {
            var name = world.Get<ItemIdentity>(item).Name;
            if (IsOnOrAdjacent(world.Get<Position>(player).Value, tile))
                verbs.Add(new WorldVerb($"Pick up the {name}",
                    (ctx, p) => PickUp(ctx, p, item),
                    new PickupItemCommand(item)));
                verbs.Add(new WorldVerb($"Examine the {name}",
                    (ctx, _) =>
                    {
                        ExamineItem(ctx, item);
                        return ActionResult.Free;
                    },
                    new ExamineEntityCommand(item)));
        }

        if (verbs.Count == 0)
            verbs.Add(ExamineTerrainVerb(context, tile));

        return verbs;
    }

    private static void AddContainerForceVerbs(IGameRuntimeContext context, List<WorldVerb> verbs,
        Entity player, Entity container, Vector2i source)
    {
        if (DoorSystem.HasItemWithFlag(context, player, "tool_smash"))
            verbs.Add(new WorldVerb("Bash it open",
                (ctx, p) => DoorSystem.ForceContainerEntry(ctx, p, container, source, "tool_smash", "bash", 12),
                new ForceContainerCommand(container, source, "tool_smash", "bash", 12)));
        if (DoorSystem.HasItemWithFlag(context, player, "tool_crowbar"))
            verbs.Add(new WorldVerb("Pry it open",
                (ctx, p) => DoorSystem.ForceContainerEntry(ctx, p, container, source, "tool_crowbar", "pry", 8),
                new ForceContainerCommand(container, source, "tool_crowbar", "pry", 8)));
        if (DoorSystem.HasItemWithFlag(context, player, "tool_lockpick"))
            verbs.Add(new WorldVerb("Pick the lock",
                (ctx, p) => DoorSystem.ForceContainerEntry(ctx, p, container, source, "tool_lockpick", "pick", 1),
                new ForceContainerCommand(container, source, "tool_lockpick", "pick", 1)));
    }

    private static void AddDoorForceVerbs(IGameRuntimeContext context, List<WorldVerb> verbs,
        Entity player, MapTransition transition, Vector2i source)
    {
        if (DoorSystem.HasItemWithFlag(context, player, "tool_smash"))
            verbs.Add(new WorldVerb("Bash the door",
                (ctx, p) => DoorSystem.ForceEntry(ctx, p, transition, source, "tool_smash", "bash", 12),
                new ForceDoorCommand(transition, source, "tool_smash", "bash", 12)));
        if (DoorSystem.HasItemWithFlag(context, player, "tool_crowbar"))
            verbs.Add(new WorldVerb("Pry the door",
                (ctx, p) => DoorSystem.ForceEntry(ctx, p, transition, source, "tool_crowbar", "pry", 8),
                new ForceDoorCommand(transition, source, "tool_crowbar", "pry", 8)));
        if (DoorSystem.HasItemWithFlag(context, player, "tool_lockpick"))
            verbs.Add(new WorldVerb("Pick the door lock",
                (ctx, p) => DoorSystem.ForceEntry(ctx, p, transition, source, "tool_lockpick", "pick", 1),
                new ForceDoorCommand(transition, source, "tool_lockpick", "pick", 1)));
    }

    public static string DescribeTargetName(IGameRuntimeContext context, Vector2i tile)
    {
        var world = context.World;
        var mapId = context.MapId;

        foreach (var entity in world.Query<Position, WorldObjectIdentity>())
            if (OccupiesTile(world, entity, mapId, tile))
                return world.Get<WorldObjectIdentity>(entity).Name;

        foreach (var entity in world.Query<Position, Actor>())
        {
            if (entity.Equals(context.Player)) continue;
            if (OccupiesTile(world, entity, mapId, tile))
                return world.Has<Named>(entity) ? world.Get<Named>(entity).Name : "Someone";
        }

        foreach (var item in ItemSystem.ItemsAt(world, mapId, tile))
            return world.Get<ItemIdentity>(item).Name;

        return context.Map[tile.X, tile.Y].Glyph != default
            ? TerrainAt(context, tile).Name
            : "Nothing";
    }

    public static void ExamineAt(IGameRuntimeContext context, Vector2i tile)
    {
        var world = context.World;
        var mapId = context.MapId;

        foreach (var entity in world.Query<Position, WorldObjectIdentity>())
            if (OccupiesTile(world, entity, mapId, tile))
            {
                ExamineWorldObject(context, entity);
                return;
            }

        foreach (var entity in world.Query<Position, Actor>())
        {
            if (entity.Equals(context.Player)) continue;
            if (OccupiesTile(world, entity, mapId, tile))
            {
                ExamineNpc(context, entity);
                return;
            }
        }

        foreach (var item in ItemSystem.ItemsAt(world, mapId, tile))
        {
            ExamineItem(context, item);
            return;
        }

        var terrain = TerrainAt(context, tile);
        Publish(context, context.Player, "interaction.examine", $"{terrain.Name}: {terrain.Description}");
    }

    public static ActionResult Attack(IGameRuntimeContext context, Entity attacker, Entity target)
    {
        var world = context.World;
        if (!world.IsAlive(target) || !world.Has<Health>(target))
            return ActionResult.Failed;

        ref var hp = ref world.Get<Health>(target);
        var isPlayer = attacker.Equals(context.Player);

        var damage = 1;
        if (isPlayer && world.Has<CharacterIdentity>(attacker) &&
            world.Get<CharacterIdentity>(attacker).Stats.TryGetValue("strength", out var strength))
        {
            damage += Math.Max(0, (strength - 8) / 4);
        }

        if (isPlayer && world.Has<Equipped>(attacker))
        {
            var equipped = world.Get<Equipped>(attacker).Item.Resolve(world);
            if (world.IsAlive(equipped) && world.Has<Damage>(equipped))
                damage = world.Get<Damage>(equipped).Amount;
        }

        var name = world.Has<Named>(target)
            ? world.Get<Named>(target).Name
            : "something";

        var location = world.Get<Position>(target).Value;
        hp.Current -= damage;
        var killed = hp.Current <= 0;
        context.Events.Publish(new SimEvent(
            killed ? "combat.kill" : "combat.hit",
            isPlayer ? $"You hit {name} for {damage} damage." : $"The attack hits {name}.",
            context.World.Get<Location>(attacker).MapId,
            new Vector2i((int)location.X, (int)location.Y),
            context.Clock.MinuteOfDay,
            world.IsAlive(attacker) ? world.StableId(attacker) : null,
            world.IsAlive(target) ? world.StableId(target) : null));

        context.Events.Publish(new SimEvent(
            "combat.message",
            isPlayer
                ? $"You hit {name} for {damage} damage."
                : $"The {world.Get<CreatureIdentity>(attacker).DefinitionId} hits {name} for {damage} damage.",
            context.World.Get<Location>(attacker).MapId,
            new Vector2i((int)location.X, (int)location.Y),
            context.Clock.MinuteOfDay));

        if (isPlayer && !world.Has<Hostile>(target))
        {
            ConsequenceSystem.Report(
                context,
                killed ? "murder" : "assault",
                $"You {(!killed ? "attacked" : "killed")} {name}",
                new Vector2i((int)location.X, (int)location.Y),
                attacker,
                target);
        }

        if (!killed)
        {
            if (isPlayer && !world.Has<Hostile>(target))
                world.Set(target, new Hostile());
            return ActionResult.Turn;
        }

        context.Events.Publish(new SimEvent(
            "combat.kill",
            $"{name} has been killed!",
            context.World.Get<Location>(attacker).MapId,
            new Vector2i((int)location.X, (int)location.Y),
            context.Clock.MinuteOfDay));
        world.Destroy(target);
        context.Scheduler.Remove(target);
        return ActionResult.Turn;
    }

    public static ActionResult PickUp(IGameRuntimeContext context, Entity player, Entity item)
    {
        if (!context.World.IsAlive(item)) return ActionResult.Failed;
        var name = context.World.Get<ItemIdentity>(item).Name;
        if (ItemSystem.TryPickup(context.World, player, item))
        {
            var position = context.World.Get<Position>(player).Value;
            context.Events.Publish(new SimEvent(
                "item.picked_up",
                $"You pick up the {name}.",
                context.World.Get<Location>(player).MapId,
                new Vector2i((int)position.X, (int)position.Y),
                context.Clock.MinuteOfDay,
                context.World.StableId(player),
                context.World.IsAlive(item) ? context.World.StableId(item) : null));
            return ActionResult.Turn;
        }

        return ActionResult.Failed;
    }

    public static void ExamineEntity(IGameRuntimeContext context, Entity entity)
    {
        if (context.World.Has<WorldObjectIdentity>(entity))
            ExamineWorldObject(context, entity);
        else if (context.World.Has<Actor>(entity))
            ExamineNpc(context, entity);
        else if (context.World.Has<ItemIdentity>(entity))
            ExamineItem(context, entity);
    }

    private static void ExamineWorldObject(IGameRuntimeContext context, Entity entity)
    {
        var identity = context.World.Get<WorldObjectIdentity>(entity);
        var def = context.Definitions.WorldObject(identity.DefinitionId);
        Publish(context, context.Player, "interaction.examine", $"{def.Name}. {def.Description}", entity);
    }

    private static void ExamineNpc(IGameRuntimeContext context, Entity entity)
    {
        var world = context.World;
        var name = world.Has<Named>(entity)
            ? world.Get<Named>(entity).Name
            : "Someone";

        var stance = world.Has<Hostile>(entity)
            ? "It looks hostile."
            : "They seem to be going about their day.";

        Publish(context, context.Player, "interaction.examine", $"{name}. {stance}", entity);
    }

    private static void ExamineItem(IGameRuntimeContext context, Entity item)
    {
        var identity = context.World.Get<ItemIdentity>(item);
        var def = context.Definitions.Item(identity.DefinitionId);
        Publish(context, context.Player, "interaction.examine", $"{identity.Name}: {def.Description}", item);
    }

    private static WorldVerb ExamineTerrainVerb(IGameRuntimeContext context, Vector2i tile)
    {
        var terrain = TerrainAt(context, tile);
        return new WorldVerb($"Examine the {terrain.Name}",
            (ctx, _) =>
            {
                Publish(ctx, ctx.Player, "interaction.examine", $"{terrain.Name}: {terrain.Description}");
                return ActionResult.Free;
            });
    }

    private static TerrainDefinition TerrainAt(IGameRuntimeContext context, Vector2i tile) =>
        context.Definitions.TerrainForIndex(context.Map[tile.X, tile.Y].TerrainDefIndex);

    private static void Publish(IGameRuntimeContext context, Entity actor, string type, string description, Entity? target = null)
    {
        if (!context.World.IsAlive(actor) || !context.World.Has<Position>(actor) || !context.World.Has<Location>(actor)) return;
        var position = context.World.Get<Position>(actor).Value;
        context.Events.Publish(new SimEvent(
            type,
            description,
            context.World.Get<Location>(actor).MapId,
            new Vector2i((int)position.X, (int)position.Y),
            context.Clock.MinuteOfDay,
            context.World.StableId(actor),
            target is { } entity && context.World.IsAlive(entity) ? context.World.StableId(entity) : null));
    }

    private static bool OccupiesTile(World world, Entity entity, string mapId, Vector2i tile)
    {
        if (!world.Has<Location>(entity) ||
            world.Get<Location>(entity).MapId != mapId)
            return false;

        var position = world.Get<Position>(entity).Value;
        return (int)position.X == tile.X && (int)position.Y == tile.Y;
    }

    private static bool IsAdjacent(Vector2 a, Vector2i b) =>
        Math.Abs((int)a.X - b.X) + Math.Abs((int)a.Y - b.Y) == 1;

    private static bool IsOnOrAdjacent(Vector2 a, Vector2i b) =>
        Math.Abs((int)a.X - b.X) <= 1 && Math.Abs((int)a.Y - b.Y) <= 1;
}

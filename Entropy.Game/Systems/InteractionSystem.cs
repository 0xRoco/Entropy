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

namespace Entropy.Game.Systems;

public record WorldVerb(string Label, Func<GameContext, Entity, ActionResult> Execute);

public static class InteractionSystem
{
    public static List<WorldVerb> GetVerbs(
        GameContext context, Entity player, Vector2i tile,
        Action<GameContext, Entity> openContainer)
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
                    (ctx, p) => DoorSystem.Unlock(ctx, p, transition)));
            }

            AddDoorForceVerbs(context, verbs, player, transition, tile);

            verbs.Add(new WorldVerb("Examine the locked door",
                (ctx, _) =>
                {
                    ctx.Log.Add("The door is locked. There may be another way in.", Color4.Yellow);
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
                            (ctx, p) => DoorSystem.UnlockContainer(ctx, p, target)));

                    AddContainerForceVerbs(context, verbs, player, target, tile);
                    verbs.Add(new WorldVerb($"Examine the locked {identity.Name}",
                        (ctx, _) =>
                        {
                            ctx.Log.Add("It is locked. There may be another way in.", Color4.Yellow);
                            return ActionResult.Free;
                        }));
                }
                else if (!world.Has<Searched>(entity))
                {
                    verbs.Add(new WorldVerb($"Search the {identity.Name}",
                        (ctx, p) => SearchSystem.Start(ctx, p, target)));
                }
                else
                {
                    verbs.Add(new WorldVerb($"Open the {identity.Name}",
                        (ctx, _) =>
                        {
                            openContainer.Invoke(ctx, target);
                            return ActionResult.Turn;
                        }));
                }
            }

            if (adjacent && def.HasFlag("bed"))
            {
                var bed = entity;
                verbs.Add(new WorldVerb($"Sleep on the {identity.Name}",
                    (ctx, p) => SleepSystem.FallAsleep(ctx, p, bed)));
            }

            verbs.Add(new WorldVerb($"Examine the {identity.Name}",
                (ctx, _) =>
                {
                    ExamineWorldObject(ctx, entity);
                    return ActionResult.Free;
                }));
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
                    (ctx, _) => Attack(ctx, player, target)));

            verbs.Add(new WorldVerb($"Talk to {name}",
                (ctx, _) =>
                {
                    ctx.Log.Add($"{name} ignores you.", Color4.LightGray);
                    return ActionResult.Free;
                }));
            verbs.Add(new WorldVerb($"Examine {name}",
                (ctx, _) =>
                {
                    ExamineNpc(ctx, target);
                    return ActionResult.Free;
                }));
        }

        foreach (var item in ItemSystem.ItemsAt(world, mapId, tile))
        {
            var name = world.Get<ItemIdentity>(item).Name;
            if (IsOnOrAdjacent(world.Get<Position>(player).Value, tile))
                verbs.Add(new WorldVerb($"Pick up the {name}",
                    (ctx, p) => PickUp(ctx, p, item)));
            verbs.Add(new WorldVerb($"Examine the {name}",
                (ctx, _) =>
                {
                    ExamineItem(ctx, item);
                    return ActionResult.Free;
                }));
        }

        if (verbs.Count == 0)
            verbs.Add(ExamineTerrainVerb(context, tile));

        return verbs;
    }

    private static void AddContainerForceVerbs(GameContext context, List<WorldVerb> verbs,
        Entity player, Entity container, Vector2i source)
    {
        if (DoorSystem.HasItemWithFlag(context, player, "tool_smash"))
            verbs.Add(new WorldVerb("Bash it open",
                (ctx, p) => DoorSystem.ForceContainerEntry(ctx, p, container, source, "tool_smash", "bash", 12)));
        if (DoorSystem.HasItemWithFlag(context, player, "tool_crowbar"))
            verbs.Add(new WorldVerb("Pry it open",
                (ctx, p) => DoorSystem.ForceContainerEntry(ctx, p, container, source, "tool_crowbar", "pry", 8)));
        if (DoorSystem.HasItemWithFlag(context, player, "tool_lockpick"))
            verbs.Add(new WorldVerb("Pick the lock",
                (ctx, p) => DoorSystem.ForceContainerEntry(ctx, p, container, source, "tool_lockpick", "pick", 1)));
    }

    private static void AddDoorForceVerbs(GameContext context, List<WorldVerb> verbs,
        Entity player, MapTransition transition, Vector2i source)
    {
        if (DoorSystem.HasItemWithFlag(context, player, "tool_smash"))
            verbs.Add(new WorldVerb("Bash the door",
                (ctx, p) => DoorSystem.ForceEntry(ctx, p, transition, source, "tool_smash", "bash", 12)));
        if (DoorSystem.HasItemWithFlag(context, player, "tool_crowbar"))
            verbs.Add(new WorldVerb("Pry the door",
                (ctx, p) => DoorSystem.ForceEntry(ctx, p, transition, source, "tool_crowbar", "pry", 8)));
        if (DoorSystem.HasItemWithFlag(context, player, "tool_lockpick"))
            verbs.Add(new WorldVerb("Pick the door lock",
                (ctx, p) => DoorSystem.ForceEntry(ctx, p, transition, source, "tool_lockpick", "pick", 1)));
    }

    public static string DescribeTargetName(GameContext context, Vector2i tile)
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

    public static void ExamineAt(GameContext context, Vector2i tile)
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
        context.Log.Add($"{terrain.Name}: {terrain.Description}", Color4.LightGray);
    }

    public static ActionResult Attack(GameContext context, Entity attacker, Entity target)
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

        context.Log.Add(isPlayer
            ? $"You hit {name} for {damage} damage."
            : $"The {world.Get<CreatureIdentity>(attacker).DefinitionId} hits {name} for {damage} damage.");

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

        context.Log.Add($"{name} has been killed!", Color4.Yellow);
        world.Destroy(target);
        context.Turns.RemoveActor(target);
        return ActionResult.Turn;
    }

    private static ActionResult PickUp(GameContext context, Entity player, Entity item)
    {
        if (!context.World.IsAlive(item)) return ActionResult.Failed;
        var name = context.World.Get<ItemIdentity>(item).Name;
        if (ItemSystem.TryPickup(context.World, player, item))
        {
            context.Log.Add($"You pick up the {name}.");
            return ActionResult.Turn;
        }

        return ActionResult.Failed;
    }

    private static void ExamineWorldObject(GameContext context, Entity entity)
    {
        var identity = context.World.Get<WorldObjectIdentity>(entity);
        var def = context.Definitions.WorldObject(identity.DefinitionId);
        context.Log.Add($"{def.Name}. {def.Description}", Color4.LightGray);
    }

    private static void ExamineNpc(GameContext context, Entity entity)
    {
        var world = context.World;
        var name = world.Has<Named>(entity)
            ? world.Get<Named>(entity).Name
            : "Someone";

        var stance = world.Has<Hostile>(entity)
            ? "It looks hostile."
            : "They seem to be going about their day.";

        context.Log.Add($"{name}. {stance}", Color4.LightGray);
    }

    private static void ExamineItem(GameContext context, Entity item)
    {
        var identity = context.World.Get<ItemIdentity>(item);
        var def = context.Definitions.Item(identity.DefinitionId);
        context.Log.Add($"{identity.Name}: {def.Description}", Color4.LightGray);
    }

    private static WorldVerb ExamineTerrainVerb(GameContext context, Vector2i tile)
    {
        var terrain = TerrainAt(context, tile);
        return new WorldVerb($"Examine the {terrain.Name}",
            (ctx, _) =>
            {
                ctx.Log.Add($"{terrain.Name}: {terrain.Description}", Color4.LightGray);
                return ActionResult.Free;
            });
    }

    private static TerrainDefinition TerrainAt(GameContext context, Vector2i tile) =>
        context.Definitions.TerrainForIndex(context.Map[tile.X, tile.Y].TerrainDefIndex);

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

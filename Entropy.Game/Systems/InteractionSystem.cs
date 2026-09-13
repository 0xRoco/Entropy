using Entropy.Content;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.UI;
using Entropy.Game.Components;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Components.ItemEffects;
using Entropy.Game.Components.Tags;
using Entropy.Game.Components.Vitals;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public record WorldVerb(string Label, bool SpendsTurn, Action<GameContext, Entity> Execute);

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

        if (transition is not null && IsAdjacent(world.Get<Position>(player).Value, tile))
        {
            if (context.LockedMaps.Contains(transition.ToMap))
            {
                if (HasItemWithFlag(context, player, "key_pharmacy"))
                {
                    verbs.Add(new WorldVerb("Unlock the door with the pharmacy key", true,
                        (ctx, p) => UnlockDoor(ctx, p, transition.ToMap)));
                }

                if (HasItemWithFlag(context, player, "tool_smash"))
                {
                    verbs.Add(new WorldVerb("Smash through the locked door", true,
                        (ctx, p) => ForceDoor(ctx, p, transition.ToMap, tile)));
                }

                verbs.Add(new WorldVerb("Examine the locked door", false,
                    (ctx, _) => ctx.Log.Add("The door is locked. There may be another way in.", Color4.Yellow)));
            }
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
                if (!world.Has<Searched>(entity))
                {
                    verbs.Add(new WorldVerb($"Search the {identity.Name}", true,
                        (ctx, _) => SearchContainer(ctx, target)));
                }
                else
                {
                    verbs.Add(new WorldVerb($"Open the {identity.Name}", true,
                        (ctx, _) => openContainer.Invoke(ctx, target)));
                }
            }

            if (adjacent && def.HasFlag("bed"))
            {
                var bed = entity;
                verbs.Add(new WorldVerb($"Sleep on the {identity.Name}", true,
                    (ctx, p) => SleepSystem.FallAsleep(ctx, p, bed)));
            }

            verbs.Add(new WorldVerb($"Examine the {identity.Name}", false,
                (ctx, _) => ExamineWorldObject(ctx, entity)));
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
                verbs.Add(new WorldVerb($"Attack {name}", true,
                    (ctx, _) => Attack(ctx, player, target)));

            verbs.Add(new WorldVerb($"Talk to {name}", false,
                (ctx, _) => ctx.Log.Add($"{name} ignores you.", Color4.LightGray)));
            verbs.Add(new WorldVerb($"Examine {name}", false,
                (ctx, _) => ExamineNpc(ctx, target)));
        }

        foreach (var item in ItemSystem.ItemsAt(world, mapId, tile))
        {
            var name = world.Get<ItemIdentity>(item).Name;
            if (IsOnOrAdjacent(world.Get<Position>(player).Value, tile))
                verbs.Add(new WorldVerb($"Pick up the {name}", true,
                    (ctx, p) => PickUp(ctx, p, item)));
            verbs.Add(new WorldVerb($"Examine the {name}", false,
                (ctx, _) => ExamineItem(ctx, item)));
        }

        if (verbs.Count == 0)
            verbs.Add(ExamineTerrainVerb(context, tile));

        return verbs;
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

    public static void Attack(GameContext context, Entity attacker, Entity target)
    {
        var world = context.World;
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
            var equipped = world.Get<Equipped>(attacker).Item;
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
            return;
        }

        context.Log.Add($"{name} has been killed!", Color4.Yellow);
        world.Destroy(target);
        context.Turns.RemoveActor(target);
    }

    private static void PickUp(GameContext context, Entity player, Entity item)
    {
        var name = context.World.Get<ItemIdentity>(item).Name;
        if (ItemSystem.TryPickup(context.World, player, item))
            context.Log.Add($"You pick up the {name}.");
    }

    private static void SearchContainer(GameContext context, Entity container)
    {
        context.World.Set(container, new Searched());
        var identity = context.World.Get<WorldObjectIdentity>(container);
        var count = context.World.Has<Container>(container)
            ? context.World.Get<Container>(container).Items.Count
            : 0;
        context.Log.Add(
            count > 0
                ? $"You search the {identity.Name}. You find something inside."
                : $"You search the {identity.Name}. It is empty.",
            count > 0 ? Color4.Cyan : Color4.LightGray);
    }

    private static void UnlockDoor(GameContext context, Entity player, string mapId)
    {
        context.LockedMaps.Remove(mapId);
        context.Log.Add("You unlock the pharmacy door quietly.", Color4.Cyan);
    }

    private static void ForceDoor(GameContext context, Entity player, string mapId, Vector2i tile)
    {
        context.LockedMaps.Remove(mapId);
        context.Log.Add("You smash the lock. The noise carries down the street.", Color4.OrangeRed);
        NoiseSystem.Emit(context, tile, 12, "Something is breaking into the pharmacy");
    }

    private static bool HasItemWithFlag(GameContext context, Entity player, string flag)
    {
        var world = context.World;
        if (!world.Has<Container>(player)) return false;

        return world.Get<Container>(player).Items
            .Where(world.IsAlive)
            .Where(item => world.Has<ItemIdentity>(item))
            .Select(item => context.Definitions.Item(world.Get<ItemIdentity>(item).DefinitionId))
            .Any(item => item.Flags.Contains(flag));
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
        return new WorldVerb($"Examine the {terrain.Name}", false,
            (ctx, _) => ctx.Log.Add($"{terrain.Name}: {terrain.Description}", Color4.LightGray));
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

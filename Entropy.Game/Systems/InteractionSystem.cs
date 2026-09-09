using Entropy.Content;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Game.Components;
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

        foreach (var entity in world.Query<Position, WorldObjectIdentity>())
        {
            if (!OccupiesTile(world, entity, mapId, tile)) continue;

            var identity = world.Get<WorldObjectIdentity>(entity);
            var adjacent = IsAdjacent(world.Get<Position>(player).Value, tile);
            if (world.Has<Container>(entity) && adjacent)
            {
                var target = entity;
                verbs.Add(new WorldVerb($"Open the {identity.Name}", true,
                    (ctx, _) => openContainer.Invoke(ctx, target)));
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

    public static void Attack(GameContext context, Entity player, Entity target)
    {
        var world = context.World;
        ref var hp = ref world.Get<Health>(target);

        var damage = 1;
        if (world.Has<Equipped>(player))
        {
            var equipped = world.Get<Equipped>(player).Item;
            if (world.IsAlive(equipped) && world.Has<Damage>(equipped))
                damage = world.Get<Damage>(equipped).Amount;
        }

        var name = world.Has<Named>(target)
            ? world.Get<Named>(target).Name
            : "something";

        var location = world.Get<Position>(target).Value;
        hp.Current -= damage;
        var killed = hp.Current <= 0;

        context.Log.Add($"You hit {name} for {damage} damage.");

        if (!world.Has<Hostile>(target))
        {
            ConsequenceSystem.Report(
                context,
                killed ? "murder" : "assault",
                $"You {(!killed ? "attacked" : "killed")} {name}",
                new Vector2i((int)location.X, (int)location.Y),
                player,
                target);
        }

        if (!killed)
        {
            if (!world.Has<Hostile>(target))
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

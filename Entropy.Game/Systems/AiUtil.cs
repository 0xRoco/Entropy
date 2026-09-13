using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components;
using Entropy.Game.Components.Tags;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public static class AiUtil
{
    public static Vector2i ToTile(Vector2 pos) => new((int)pos.X, (int)pos.Y);

    public static int Manhattan(Vector2 a, Vector2 b) =>
        Math.Abs((int)a.X - (int)b.X) + Math.Abs((int)a.Y - (int)b.Y);

    public static bool IsAdjacent(Vector2 a, Vector2 b) => Manhattan(a, b) == 1;

    /// <summary>
    /// Any other Actor on the same map occupying the tile (creatures block
    /// creatures). Map is derived from the mover's own Location.
    /// </summary>
    public static bool IsOccupied(World world, Entity self, Vector2i tile)
    {
        var mapId = world.Get<Location>(self).MapId;

        foreach (var e in world.Query<Position, Actor>())
        {
            if (e.Equals(self)) continue;
            if (!world.Has<Location>(e) || world.Get<Location>(e).MapId != mapId) continue;
            var p = world.Get<Position>(e).Value;
            if ((int)p.X == tile.X && (int)p.Y == tile.Y) return true;
        }

        return false;
    }

    /// <summary>
    /// Pathfind one step toward goal; moves only if the step is free
    /// </summary>
    public static bool StepToward(World world, Entity self, TileMap map, Vector2i goal)
    {
        ref var pos = ref world.Get<Position>(self);
        var start = ToTile(pos.Value);

        var step = Pathfinding.NextStep(start, goal, map.Width, map.Height,
            (x, y) => map[x, y].Walkable);

        if (step == null || step.Value.Equals(start)) return false;
        if (IsOccupied(world, self, step.Value)) return false;

        pos.Value = new Vector2(step.Value.X, step.Value.Y);
        return true;
    }

    /// <summary>
    /// Random adjacent walkable free tile, or stay put
    /// </summary>
    public static bool Wander(World world, Entity self, TileMap map, Rng rng, float chance)
    {
        if (!rng.Chance(chance)) return false;

        ref var pos = ref world.Get<Position>(self);
        var start = ToTile(pos.Value);

        var options = new List<Vector2i>
        {
            new(start.X, start.Y - 1), new(start.X, start.Y + 1),
            new(start.X - 1, start.Y), new(start.X + 1, start.Y)
        };

        for (var i = options.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (options[i], options[j]) = (options[j], options[i]);
        }

        foreach (var tile in options)
        {
            if (tile.X < 0 || tile.X >= map.Width || tile.Y < 0 || tile.Y >= map.Height) continue;
            if (!map[tile.X, tile.Y].Walkable) continue;
            if (IsOccupied(world, self, tile)) continue;
            pos.Value = new Vector2(tile.X, tile.Y);
            return true;
        }

        return false;
    }

    /// <summary>Random adjacent step, but never beyond radius from the anchor.</summary>
    public static bool WanderNear(World world, Entity self, TileMap map, Rng rng,
        Vector2i anchor, int radius, float chance)
    {
        if (!rng.Chance(chance)) return false;

        ref var pos = ref world.Get<Position>(self);
        var start = ToTile(pos.Value);

        var options = new List<Vector2i>
        {
            new(start.X, start.Y - 1), new(start.X, start.Y + 1),
            new(start.X - 1, start.Y), new(start.X + 1, start.Y)
        };

        for (var i = options.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (options[i], options[j]) = (options[j], options[i]);
        }

        foreach (var tile in options)
        {
            if (tile.X < 0 || tile.X >= map.Width || tile.Y < 0 || tile.Y >= map.Height) continue;
            if (Math.Abs(tile.X - anchor.X) + Math.Abs(tile.Y - anchor.Y) > radius) continue;
            if (!map[tile.X, tile.Y].Walkable) continue;
            if (IsOccupied(world, self, tile)) continue;
            pos.Value = new Vector2(tile.X, tile.Y);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Moves an NPC toward an anchor, crossing map transitions when necessary.
    /// The player transition remains in TurnProcessor because it also changes
    /// the active map/FOV/camera context.
    /// </summary>
    public static bool TravelToward(
        World world,
        Entity self,
        GameContext context,
        string destinationMapId,
        Vector2i destinationTile)
    {
        if (!world.Has<Location>(self))
            return false;

        var currentMapId = world.Get<Location>(self).MapId;
        var currentMap = context.Maps[currentMapId];

        ref var position = ref world.Get<Position>(self);
        var here = ToTile(position.Value);

        if (currentMapId == destinationMapId)
            return StepToward(world, self, currentMap, destinationTile);

        var transition = context.Maps.NextTransitionToward(currentMapId, destinationMapId);
        if (transition == null)
            return false;

        if (here == transition.FromTile)
        {
            ApplyTransition(world, self, transition);
            return true;
        }

        return StepToward(world, self, currentMap, transition.FromTile);
    }

    public static void ApplyTransition(World world, Entity entity, MapTransition transition)
    {
        world.Set(entity, new Location { MapId = transition.ToMap });
        world.Get<Position>(entity).Value =
            new Vector2(transition.ToTile.X, transition.ToTile.Y);
    }
}
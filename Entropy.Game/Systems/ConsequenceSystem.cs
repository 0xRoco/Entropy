using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public static class ConsequenceSystem
{
    private static VisibilityMap? _scratch;

    public static void Report(GameContext ctx, string type, string description, Vector2i location, Entity attacker, Entity victim)
    {
        var simEvent = new SimEvent(
            type,
            description,
            ctx.MapId,
            location,
            ctx.Clock.MinuteOfDay);
        var witnesses = FindWitnesses(ctx, location, attacker);
        if (witnesses.Count == 0) return; // unseen crime

        var reported = false;
        foreach (var witness in witnesses)
        {
            foreach (var entity in ctx.World.Query<Position, Perception>())
            {
                if (entity.Equals(attacker) || !ctx.World.IsAlive(entity))
                    continue;

                if (!ctx.World.Has<WitnessMemory>(entity))
                    continue;

                if (!ctx.World.Has<Location>(entity) ||
                    ctx.World.Get<Location>(entity).MapId != ctx.MapId)
                    continue;

                ctx.World.Get<WitnessMemory>(witness).Witnessed.Add(simEvent);
            
                if (witness.Equals(victim)) continue; // victim doesn't report

                if (reported || ctx.World.Has<Hostile>(witness)) continue;

                reported = true;
                var name = ctx.World.Has<Named>(witness)
                    ? ctx.World.Get<Named>(witness).Name
                    : "Someone";
                ctx.Log.Add($"{name} witnessed the {type} and calls the police!", Color4.Yellow);

                ctx.World.Set(attacker, new Wanted { Crime = simEvent });
            }
        }

        if (reported)
            Dispatch(ctx, simEvent);
    }

    private static List<Entity> FindWitnesses(GameContext ctx, Vector2i location, Entity attacker)
    {
        var result = new List<Entity>();
        var map = ctx.Map;

        _scratch ??= new VisibilityMap(map.Width, map.Height);

        foreach (var entity in ctx.World.Query<Position, Perception>())
        {
            if (entity.Equals(attacker) || !ctx.World.IsAlive(entity)) continue;
            if (!ctx.World.Has<WitnessMemory>(entity)) continue;
            // witnesses never cross maps
            if (!ctx.World.Has<Location>(entity)
                || ctx.World.Get<Location>(entity).MapId != ctx.MapId) continue;

            var pos = ctx.World.Get<Position>(entity).Value;
            var posI = new Vector2i((int)pos.X, (int)pos.Y);
            var radius = ctx.World.Get<Perception>(entity).SightRadius;

            if (AiUtil.Manhattan(pos, new Vector2(location.X, location.Y)) > radius) continue;

            _scratch.ClearVisible();
            Fov.Compute(posI, radius, map, _scratch);
            if (!_scratch.IsVisible(location.X, location.Y)) continue;

            result.Add(entity);
        }

        return result;
    }

    private static void Dispatch(GameContext ctx, SimEvent simEvent)
    {
        var edge = FindEdgeSpawn(ctx.Map, simEvent.Location, ctx.Rng);
        var cop = EntitySpawner.CreateCop(
            ctx.World,
            ctx.MapId,
            ctx.Definitions.Creature("human_cop"),
            edge.X,
            edge.Y,
            ctx.Player);
        ctx.Turns.AddActor(cop);
        ctx.Log.Add("A police officer is responding.", Color4.LightGray);
    }

    public static Vector2i FindEdgeSpawn(TileMap map, Vector2i scene, Rng rng)
    {
        var candidates = new List<Vector2i>();
        for (var x = 0; x < map.Width; x++)
        {
            if (map[x, 1].Walkable) candidates.Add(new Vector2i(x, 1));
            if (map[x, map.Height - 2].Walkable) candidates.Add(new Vector2i(x, map.Height - 2));
        }
        for (var y = 0; y < map.Height; y++)
        {
            if (map[1, y].Walkable) candidates.Add(new Vector2i(1, y));
            if (map[map.Width - 2, y].Walkable) candidates.Add(new Vector2i(map.Width - 2, y));
        }

        if (candidates.Count == 0)
        {
            for (var y = 1; y < map.Height -1; y++)
                for (var x = 1; x < map.Width - 1; x++)
                    if (map[x, y].Walkable)
                        candidates.Add(new Vector2i(x, y));
        }
        
        if (candidates.Count == 0) return scene;

        // prefer spawns far from the scene
        var far = candidates
            .OrderByDescending(c => Math.Abs(c.X - scene.X) + Math.Abs(c.Y - scene.Y))
            .Take(Math.Max(1, candidates.Count / 4))
            .ToList();
        return far[rng.Next(far.Count)];
    }

    public static Vector2i FindHoldingTile(TileMap map)
    {
        for (var y = 1; y < map.Height; y++)
        for (var x = 1; x < map.Width; x++)
            if (map[x, y].Walkable)
                return new Vector2i(x, y);
        return new Vector2i(1, 1);
    }
}
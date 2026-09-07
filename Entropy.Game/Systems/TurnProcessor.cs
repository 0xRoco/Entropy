using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public class TurnProcessor
{
    private const int ActionCost = 100;
    private const int DefaultSpeed = 100;

    private readonly List<Entity> _actors = [];
    private readonly Dictionary<Entity, int> _energy = [];

    public void AddActor(Entity entity)
    {
        if (_actors.Contains(entity)) return;
        _actors.Add(entity);
        _energy[entity] = 0;
    }

    public void RemoveActor(Entity entity)
    {
        _actors.Remove(entity);
        _energy.Remove(entity);
    }

    public bool ProcessPlayerTurn(
        Entity player,
        Vector2i move,
        GameContext context,
        VisibilityMap visibility,
        int viewRadius)
    {
        ref var pos = ref context.World.Get<Position>(player);
        var target = pos.Value + move;
        var tx = (int)target.X;
        var ty = (int)target.Y;

        if (tx < 0 || tx >= context.Map.Width || ty < 0 || ty >= context.Map.Height)
            return false;

        if (!context.Map[tx, ty].Walkable)
            return false;

        var playerMapId = context.World.Get<Location>(player).MapId;

        // Only actors on the player's current map can block or be attacked.
        foreach (var entity in context.World.Query<Position, Actor>())
        {
            if (context.World.Get<Position>(entity).Value != target)
                continue;

            if (!context.World.Has<Location>(entity) ||
                context.World.Get<Location>(entity).MapId != playerMapId)
                continue;

            if (!context.World.Has<Health>(entity))
            {
                context.Log.Add("Something blocks your way", Color4.LightGray);
                Spend(player);
                return true;
            }

            ref var hp = ref context.World.Get<Health>(entity);
            var damage = 1;

            if (context.World.Has<Equipped>(player))
            {
                var equipped = context.World.Get<Equipped>(player).Item;

                if (context.World.IsAlive(equipped) &&
                    context.World.Has<Damage>(equipped))
                {
                    damage = context.World.Get<Damage>(equipped).Amount;
                }
            }

            var victimName = context.World.Has<Named>(entity)
                ? context.World.Get<Named>(entity).Name
                : "something";

            var location = new Vector2i(tx, ty);

            hp.Current -= damage;
            var killed = hp.Current <= 0;

            context.Log.Add($"You hit {victimName} for {damage} damage.");

            if (!context.World.Has<Hostile>(entity))
            {
                ConsequenceSystem.Report(
                    context,
                    killed ? "murder" : "assault",
                    $"You {(!killed ? "attacked" : "killed")} {victimName}",
                    location,
                    player,
                    entity);
            }

            if (killed)
            {
                context.Log.Add($"{victimName} has been killed!", Color4.Yellow);
                context.World.Destroy(entity);
                RemoveActor(entity);
            }

            Spend(player);
            return true;
        }

        pos.Value = target;

        var transition = context.Maps.TransitionAt(playerMapId, new Vector2i(tx, ty));

        if (transition != null)
        {
            pos.Value = new Vector2(transition.ToTile.X, transition.ToTile.Y);

            context.World.Set(player, new Location
            {
                MapId = transition.ToMap
            });

            context.MapId = transition.ToMap;
            context.Map = context.Maps[transition.ToMap];
            context.Visibility = context.Visibilities[transition.ToMap];

            Fov.Compute(
                transition.ToTile,
                viewRadius,
                context.Map,
                context.Visibility);

            Spend(
                player,
                context.Map[transition.ToTile.X, transition.ToTile.Y].MoveCost);

            return true;
        }

        Fov.Compute(new Vector2i(tx, ty), viewRadius, context.Map, visibility);
        Spend(player, context.Map[tx, ty].MoveCost);
        return true;
    }

    public void RunAITurns(Entity player, GameContext ctx)
    {
        for (var i = _actors.Count - 1; i >= 0; i--)
            if (!ctx.World.IsAlive(_actors[i]))
                RemoveActor(_actors[i]);

        var perceivers = ctx.World.Query<Position, Perception>()
            .Where(entity =>
                ctx.World.Has<Location>(entity) &&
                ctx.World.Get<Location>(entity).MapId == ctx.MapId)
            .ToList();

        var candidates = ctx.World.Query<Position, Actor>()
            .Where(entity =>
                ctx.World.Has<Location>(entity) &&
                ctx.World.Get<Location>(entity).MapId == ctx.MapId)
            .ToList();

        PerceptionSystem.Update(ctx.World, ctx.Map, perceivers, candidates);

        foreach (var actor in _actors)
            _energy[actor] = _energy.GetValueOrDefault(actor) + SpeedOf(ctx, actor);

        var acted = true;
        while (acted)
        {
            acted = false;
            foreach (var actor in _actors.ToList())
            {
                if (actor.Equals(player) || !ctx.World.IsAlive(actor)) continue;
                if (_energy.GetValueOrDefault(actor) < ActionCost) continue;

                _energy[actor] -= ActionCost;
                if (ctx.World.Has<Behavior>(actor))
                    ctx.World.Get<Behavior>(actor).Impl.Act(ctx.World, actor, ctx);

                if (!ctx.World.IsAlive(player)) return;
                acted = true;
            }
        }
    }

    private void Spend(Entity entity, int amount = 100)
    {
        if (_energy.ContainsKey(entity))
            _energy[entity] -= amount;
    }

    private static int SpeedOf(GameContext ctx, Entity entity) =>
        ctx.World.Has<Speed>(entity) ? ctx.World.Get<Speed>(entity).Value : DefaultSpeed;
}
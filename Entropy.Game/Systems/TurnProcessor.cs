using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public class TurnProcessor
{
    public TurnQueue Queue => _queue;
    
    private readonly TurnQueue _queue = new();
    private const int PlayerDamage = 1;
    
    public void AddActor(Entity entity) => _queue.Add(entity);
    public void RemoveActor(Entity entity) => _queue.Remove(entity);
    
    public bool ProcessPlayerTurn(Entity player, Vector2i move, GameContext context, VisibilityMap visibility, int viewRadius)
    {
        ref var pos = ref context.World.Get<Position>(player);
        var target = pos.Value + move;
        var tx = (int)target.X;
        var ty = (int)target.Y;
        
        if (tx < 0 || tx >= context.Map.Width || ty < 0 || ty >= context.Map.Height) return false;
        if (!context.Map[tx, ty].Walkable) return false;

        foreach (var entity in context.World.Query<Position, Actor>())
        {
            if (context.World.Get<Position>(entity).Value != target) continue;

            if (!context.World.Has<Hostile>(entity))
            {
                context.Log.Add("Something blocks your way", Color4.LightGray);
                return true;
            }
            
            ref var hp = ref context.World.Get<Health>(entity);
            hp.Current -= PlayerDamage;
            context.Log.Add($"You hit the zombie for {PlayerDamage} damage.");
            if (hp.Current > 0) return true;
            
            context.Log.Add("The zombie has been killed!", Color4.Yellow);
            context.World.Destroy(entity);
            _queue.Remove(entity);
            return true;
        }
        
        pos.Value = target;
        Fov.Compute(new Vector2i(tx, ty), viewRadius, context.Map, visibility);
        return true;
    }
    
    public void RunAITurns(Entity player, GameContext ctx)
    {
        _queue.Advance();

        var perceivers = ctx.World.Query<Position, Perception>().ToList();
        var candidates = ctx.World.Query<Position, Actor>().ToList();
        PerceptionSystem.Update(ctx.World, ctx.Map, perceivers, candidates);

        while (!_queue.GetCurrent.Equals(player))
        {
            var actor = _queue.GetCurrent;

            if (ctx.World.IsAlive(actor) && ctx.World.Has<Behavior>(actor))
                ctx.World.Get<Behavior>(actor).Impl.Act(ctx.World, actor, ctx);

            // if player died during AI turns, break out of the loop
            if (!ctx.World.IsAlive(player)) break;

            _queue.Advance();
        }
    }
}
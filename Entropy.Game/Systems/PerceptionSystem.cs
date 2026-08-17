using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.World;
using Entropy.Game.Components;
using OpenTK.Mathematics;

namespace Entropy.Game.Systems;

public static class PerceptionSystem
{
    public static void Update(World world, TileMap map, IReadOnlyList<Entity> perceivers,
        IReadOnlyList<Entity> candidates)
    {
        var scratch = new VisibilityMap(map.Width, map.Height);
        var candidatePosition = new Dictionary<Entity, Vector2i>(candidates.Count);

        foreach (var c in candidates)
        {
            if (!world.IsAlive(c) || !world.Has<Position>(c)) continue;
            var p = world.Get<Position>(c).Value;
            candidatePosition[c] = new Vector2i((int)p.X, (int)p.Y);
        }

        foreach (var perceiver in perceivers)
        {
            if (!world.IsAlive(perceiver)) continue;
            
            ref var perception = ref world.Get<Perception>(perceiver);
            ref var awareness = ref world.Get<Awareness>(perceiver);
            
            PruneDead(world, awareness.Detected);
            PruneDead(world, awareness.LastKnownPositions);
            PruneDead(world, awareness.TurnsSinceDetected);
            
            var pos = world.Get<Position>(perceiver).Value;
            var posI = new Vector2i((int)pos.X, (int)pos.Y);

            var sightComputed = false;

            foreach (var (candidate, candidatePos) in candidatePosition)
            {
                if (candidate.Equals(perceiver)) continue; // don't perceive self
                
                var manhattan = MathF.Abs(candidatePos.X - posI.X) 
                                + MathF.Abs(candidatePos.Y - posI.Y);

                var detected = false;

                if (perception.SightRadius > 0 && manhattan <= perception.SightRadius)
                {
                    if (!sightComputed)
                    {
                        Fov.Compute(posI, perception.SightRadius, map, scratch);
                        sightComputed = true;
                    }
                    
                    detected |= scratch.IsVisible(candidatePos.X, candidatePos.Y);
                }

                if (perception.SmellRadius > 0 && manhattan <= perception.SmellRadius) detected = true;

                if (detected)
                {
                    awareness.Detected[candidate] = true;
                    awareness.LastKnownPositions[candidate] = candidatePos;
                    awareness.TurnsSinceDetected[candidate] = 0;
                }else if (awareness.Detected.Remove(candidate))
                {
                    awareness.TurnsSinceDetected[candidate] = awareness.TurnsSinceDetected.TryGetValue(candidate, out var turns) ? turns + 1 : 1;
                }
                else
                {
                    // never detected or already lost - keep counting if we have memory
                    
                    if (awareness.LastKnownPositions.ContainsKey(candidate))
                        awareness.TurnsSinceDetected[candidate] = awareness.TurnsSinceDetected.TryGetValue(candidate, out var turns) ? turns + 1 : 1;
                }
            }
        }
    }

    private static void PruneDead<TValue>(World world, Dictionary<Entity, TValue> dict)
    {
        List<Entity> dead = null!;
        foreach (var key in dict.Keys.Where(key => !world.IsAlive(key)))
        {
            (dead ??= []).Add(key);
        }
        
        if (dead == null) return;
        foreach (var e in dead)
        {
            dict.Remove(e);
        }
    }
}
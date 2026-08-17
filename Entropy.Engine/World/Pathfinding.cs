using OpenTK.Mathematics;

namespace Entropy.Engine.World;

public static class Pathfinding
{

    private static readonly Vector2i[] Directions =
    [
        new (0, -1), // Up
        new (0, 1),  // Down
        new (-1, 0), // Left
        new (1, 0)   // Right
    ];

    public static Vector2i? NextStep(
        Vector2i start, Vector2i goal,
        int width, int height,
        Func<int, int, bool> isWalkable)
    {
        if (start == goal) return null;

        var cameFrom = new Dictionary<Vector2i, Vector2i> { [start] = start };
        var queue = new Queue<Vector2i>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == goal)
            {
                var step = current;
                while (cameFrom[step] != start) step = cameFrom[step];
                return step;
            }

            foreach (var direction in Directions)
            {
                var next = new Vector2i(current.X + direction.X, current.Y + direction.Y);
                if (next.X < 0 || next.X >= width || next.Y < 0 || next.Y >= height) continue;
                if (!isWalkable(next.X, next.Y)) continue;
                if (cameFrom.ContainsKey(next)) continue;
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }
        
        return null;
    }
}
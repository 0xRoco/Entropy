using Entropy.Engine.ECS;
using OpenTK.Mathematics;

namespace Entropy.Game.Components.AI;

public struct Awareness
{
    public Dictionary<Entity, bool> Detected; // Who can i see right now? (Current turn)
    public Dictionary<Entity, Vector2i> LastKnownPositions; // Where i saw them last? (Memory)
    public Dictionary<Entity, int> TurnsSinceDetected; // How long since i last saw them? (Staleness)

    public static Awareness Create() => new Awareness
    {
        Detected = new Dictionary<Entity, bool>(),
        LastKnownPositions = new Dictionary<Entity, Vector2i>(),
        TurnsSinceDetected = new Dictionary<Entity, int>()
    };
}

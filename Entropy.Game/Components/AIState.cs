using Entropy.Engine.ECS;
using OpenTK.Mathematics;

namespace Entropy.Game.Components;

public struct AIState
{
    public AIMode Mode = AIMode.Idle;
    public Entity? Target;
    public Vector2i? Destination;
    public Vector2i? LastKnownPosition;
    public int AlertLevel;
    public int TurnsSinceContact;
    
    public AIState() {}
}

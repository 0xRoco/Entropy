using OpenTK.Mathematics;

namespace Entropy.Simulation;

public record SimEvent(
    string Type,
    string Description,
    string MapId,
    Vector2i Location,
    int Minute,
    long? ActorStableId = null,
    long? TargetStableId = null);

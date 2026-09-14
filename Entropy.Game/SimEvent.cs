using OpenTK.Mathematics;

namespace Entropy.Game;

public record SimEvent(
    string Type,
    string Description,
    string MapId,
    Vector2i Location,
    int Minute,
    long? ActorStableId = null,
    long? TargetStableId = null);

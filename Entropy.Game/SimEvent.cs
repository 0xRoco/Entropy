using OpenTK.Mathematics;

namespace Entropy.Game;

public record SimEvent(
    string Type,
    string Description,
    Vector2i Location,
    int Minute
    );
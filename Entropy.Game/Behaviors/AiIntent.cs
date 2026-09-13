using Entropy.Engine.ECS;
using OpenTK.Mathematics;

namespace Entropy.Game.Behaviors;

public enum AiIntentType
{
    None,
    Attack,
    StepToward,
    Wander,
    WanderNear,
    TravelToward,
    Arrest,
    Despawn
}

public readonly record struct AiIntent(
    AiIntentType Type,
    Entity? Target = null,
    string? MapId = null,
    Vector2i? Destination = null,
    Vector2i? Anchor = null,
    int Radius = 0,
    float Chance = 1f);

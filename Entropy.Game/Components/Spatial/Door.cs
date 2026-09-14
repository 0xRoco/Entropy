using OpenTK.Mathematics;

namespace Entropy.Game.Components.Spatial;

public readonly record struct DoorKey(
    string FromMap,
    Vector2i FromTile,
    string ToMap,
    Vector2i ToTile);

public sealed record DoorDefinition(
    string Id,
    string RequiredKeyFlag,
    string RequiredToolFlag,
    bool Trespass = false);

public struct DoorState
{
    public bool Locked;
    public bool Broken;
    public bool TrespassReported;
}

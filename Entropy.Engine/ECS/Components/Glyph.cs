using OpenTK.Mathematics;

namespace Entropy.Engine.ECS.Components;

public struct Glyph
{
    public char Character;
    public Color4 Foreground;
    public bool RememberedInFog;
}
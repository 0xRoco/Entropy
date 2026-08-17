using OpenTK.Mathematics;

namespace Entropy.Engine.Rendering;

public static class ColorExt
{
    public static Color4 Scaled(this Color4 c, float factor) =>
        new(c.R * factor, c.G * factor, c.B * factor, c.A);
}
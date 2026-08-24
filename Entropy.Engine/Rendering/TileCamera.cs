using OpenTK.Mathematics;

namespace Entropy.Engine.Rendering;

public class TileCamera : Camera
{
    public override Matrix4 GetProjection() => Matrix4.CreateOrthographicOffCenter(
        0, ViewportSize.X / TilePixelSize,
        ViewportSize.Y / TilePixelSize, 0,
        -1f, 1f);
}
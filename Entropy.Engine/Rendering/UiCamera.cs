using OpenTK.Mathematics;

namespace Entropy.Engine.Rendering;

public class UiCamera : Camera
{
    public override Matrix4 GetProjection() => Matrix4.CreateOrthographicOffCenter(0, ViewportSize.X, ViewportSize.Y,
        0, -1f, 1f);
}
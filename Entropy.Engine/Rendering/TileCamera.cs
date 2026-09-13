using OpenTK.Mathematics;

namespace Entropy.Engine.Rendering;

public class TileCamera : Camera
{
    public float CellPixelWidth { get; set; } = TilePixelSize;
    public float CellPixelHeight { get; set; } = TilePixelSize;

    public override Matrix4 GetProjection() => Matrix4.CreateOrthographicOffCenter(
        0, ViewportSize.X / CellPixelWidth,
        ViewportSize.Y / CellPixelHeight, 0,
        -1f, 1f);
}

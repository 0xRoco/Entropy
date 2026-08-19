using OpenTK.Mathematics;

namespace Entropy.Engine.Rendering;

public class Camera
{
    public Vector2 Position;
    public float Zoom = 1f;
    public Vector2i ViewportSize;
    public const float TilePixelSize = 16f;
    
    public Vector2 WorldToScreen(Vector2 world)
    {
        var centerPixels = ViewportSize.ToVector2() * 0.5f;
        var offset = world - Position;
        var pixels = offset * (TilePixelSize * Zoom);
        return centerPixels + pixels;
    }

    public Vector2 ScreenToWorld(Vector2 screen)
    {
        var centerPixels = ViewportSize.ToVector2() * 0.5f;
        var pixelsFromCenter = screen - centerPixels;
        return pixelsFromCenter / (TilePixelSize * Zoom) + Position;
    }

    public Vector2 VisibleHalfExtents => ViewportSize.ToVector2() / (TilePixelSize * Zoom * 2f);

    public (Vector2 Min, Vector2 Max) VisibleWorldBounds()
    {
        var half = VisibleHalfExtents;
        return (Position - half, Position + half);
    }

    public virtual Matrix4 GetProjection()
    {
        var (min, max) = VisibleWorldBounds();
        return Matrix4.CreateOrthographicOffCenter(min.X, max.X, max.Y, min.Y, -1f, 1f);    }
}
using OpenTK.Mathematics;

namespace Entropy.Engine.Rendering;

public class Camera
{
    public Vector2 Position;
    public Vector2i ViewportOrigin;
    public float Zoom = 1f;
    public Vector2i ViewportSize;
    public const float TilePixelSize = 16f;
    
    public Vector2 WorldToScreen(Vector2 world)
    {
        var centerPixels = ViewportOrigin.ToVector2() + ViewportSize.ToVector2() * 0.5f;
        var offset = world - Position;
        return centerPixels + offset * (TilePixelSize * Zoom);
    }


    public Vector2 ScreenToWorld(Vector2 screen)
    {
        var centerPixels = ViewportOrigin.ToVector2() + ViewportSize.ToVector2() * 0.5f;
        var pixelsFromCenter = screen - centerPixels;
        return pixelsFromCenter / (TilePixelSize * Zoom) + Position;
    }
    
    public bool ContainsScreenPoint(Vector2 screen) =>
        screen.X >= ViewportOrigin.X &&
        screen.X < ViewportOrigin.X + ViewportSize.X &&
        screen.Y >= ViewportOrigin.Y &&
        screen.Y < ViewportOrigin.Y + ViewportSize.Y;

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
using Entropy.Engine.Core;
using Entropy.Engine.Rendering;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.Systems;

public static class Controls
{
    public static Vector2i? GetMoveDirection(IGameInput input)
    {
        if (input.IsKeyPressed(Keys.Up))    return new Vector2i(0, -1);
        if (input.IsKeyPressed(Keys.Down))  return new Vector2i(0, 1);
        if (input.IsKeyPressed(Keys.Left))  return new Vector2i(-1, 0);
        if (input.IsKeyPressed(Keys.Right)) return new Vector2i(1, 0);
        return null;
    }

    public static Vector2 GetCameraPan(IGameInput input, float dt)
    {
        const float speed = 5f;
        var pan = Vector2.Zero;
        if (input.IsKeyDown(Keys.W)) pan += new Vector2(0, -speed);
        if (input.IsKeyDown(Keys.S)) pan += new Vector2(0, speed);
        if (input.IsKeyDown(Keys.A)) pan += new Vector2(-speed, 0);
        if (input.IsKeyDown(Keys.D)) pan += new Vector2(speed, 0);

        if (pan.LengthSquared > 0)
        {
            pan = pan.Normalized() * dt * 5f;
        }

        return pan;
    }

    public static float GetZoomDelta(IGameInput input, float cameraZoom)
    {
        var wheel = input.MouseWheelDelta;        
        if (wheel != 0)
        {
            cameraZoom *= wheel > 0 ? 1.1f : 1/1.1f;
            cameraZoom = Math.Clamp(cameraZoom, 0.25f, 4f);
        }
        return cameraZoom;
    }

    public static Vector2? GetInspectedTile(IGameInput input, Camera camera)
    {
        return GetClickedTile(input, camera, MouseButton.Left);
    }

    public static Vector2? GetClickedTile(IGameInput input, Camera camera, MouseButton button)
    {
        if (input.IsMouseButtonPressed(button))
        {
            var mouseScreen = input.MousePosition;
            if (!camera.ContainsScreenPoint(mouseScreen))
                return null;
            var mouseWorld = camera.ScreenToWorld(mouseScreen);
            var tx = (int)Math.Floor(mouseWorld.X);
            var ty = (int)Math.Floor(mouseWorld.Y);

            return new Vector2(tx, ty);
        }

        return null;
    }
}
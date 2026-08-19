using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Engine.Core;

public interface IGameInput
{
    bool IsKeyDown(Keys key);
    bool IsKeyPressed(Keys key);
    Keys? GetKeyPressed();
    bool IsMouseButtonDown(MouseButton button);
    bool IsMouseButtonPressed(MouseButton button);
    Vector2 MousePosition { get; }
    float MouseWheelDelta { get; }
}
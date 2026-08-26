using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Engine.Core;

public class Window(IGameClient client, WindowSettings settings) : GameWindow(GameWindowSettings.Default,
    new NativeWindowSettings
    {
        Title = settings.Title,
        ClientSize = new Vector2i(settings.Width, settings.Height),
        APIVersion = new Version(3, 3),
        Flags = ContextFlags.Default,
        Profile = ContextProfile.Core,
        Vsync = settings.VSync ? VSyncMode.On : VSyncMode.Off,
    }), IGameInput
{
    protected override void OnLoad()
    {
        base.OnLoad();
        Console.WriteLine($"GL {GL.GetString(StringName.Version)}, Renderer {GL.GetString(StringName.Renderer)}, {ClientSize.X}x{ClientSize.Y}");
        GL.ClearColor(Color4.Black);
        client.Load(ClientSize, this);
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        client.Dispose();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        client.Resize(e.Width, e.Height);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        client.Render(args);
        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        client.Update(args);
        if (client.ExitRequested) Close();
    }
    
    public new bool IsKeyDown(Keys key) => KeyboardState.IsKeyDown(key);
    public new bool IsKeyPressed(Keys key) => KeyboardState.IsKeyPressed(key);
    public Keys? GetKeyPressed()
    {
        foreach (var key in Enum.GetValues<Keys>())
        {
            if ((int)key < 0 || (int)key > 348) continue; 
            if (KeyboardState.IsKeyPressed(key)) return key;
        }
        return null;
    }

    public new bool IsMouseButtonDown(MouseButton button) => MouseState.IsButtonDown(button);
    public new bool IsMouseButtonPressed(MouseButton button) => MouseState.IsButtonPressed(button);
    public new Vector2 MousePosition => MouseState.Position;
    public float MouseWheelDelta => MouseState.ScrollDelta.Y;
}
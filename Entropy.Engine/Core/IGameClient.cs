using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Entropy.Engine.Core;

public interface IGameClient : IDisposable
{
    void Load(Vector2i clientSize, IGameInput input);
    void Update(FrameEventArgs args);
    void Render(FrameEventArgs args);
    void Resize(int width, int height);
}
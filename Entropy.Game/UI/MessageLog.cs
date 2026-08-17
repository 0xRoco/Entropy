using OpenTK.Mathematics;

namespace Entropy.Game.UI;

public class MessageLog(int capacity = 100)
{
    private readonly List<(string Text, Color4 Color)> _messages = [];

    public void Add(string text, Color4? color = null)
    {
        _messages.Add((text, color ?? Color4.White));
        if (_messages.Count > capacity)
            _messages.RemoveAt(0);
    }
    
    public IReadOnlyList<(string Text, Color4 Color)> Messages => _messages.AsReadOnly();
    public IReadOnlyList<(string Text, Color4 Color)> GetRecent(int count) =>
        [.. _messages.Skip(Math.Max(0, _messages.Count - count))];
}
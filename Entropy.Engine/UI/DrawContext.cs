using Entropy.Engine.Rendering;
using OpenTK.Mathematics;

namespace Entropy.Engine.UI;

public class DrawContext
{
    public required QuadBatcher Batcher { get; init; }
    public required GlyphAtlas Atlas { get; init; }
    
    public Color4 DefaultForeground { get; init; } = UiTheme.Text;
    
    private readonly Stack<UiRect> _clips = new();

    public void DrawGlyph(int tileX, int tileY, Color4 color, char glyph)
    {
        if (IsClipped(tileX, tileY)) return;
        Batcher.AddTexturedQuad(tileX, tileY, 1, 1, color, glyph);
    }
    public void DrawGlyph(float tileX, float tileY, Color4 color, char glyph) =>
        Batcher.AddTexturedQuad(tileX, tileY, 1, 1, color, glyph);
    public void DrawRect(int tileX, int tileY, int width, int height, Color4 color) =>
        DrawRect((float)tileX, tileY, width, height, color);
    public void DrawRect(float tileX, float tileY, float width, float height, Color4 color)
    {
        if (_clips.Count == 0)
        {
            Batcher.AddRect(tileX, tileY, width, height, color);
            return;
        }

        var clip = _clips.Peek();

        var x1 = Math.Max(tileX, clip.X);
        var y1 = Math.Max(tileY, clip.Y);
        var x2 = Math.Min(tileX + width, clip.Right);
        var y2 = Math.Min(tileY + height, clip.Bottom);

        if (x2 <= x1 || y2 <= y1) return;

        Batcher.AddRect(x1, y1, x2 - x1, y2 - y1, color);
    }
    public void DrawText(int tileX, int tileY, string text, Color4 color = default)
    {
        if (color == default)
            color = DefaultForeground;
        
        for (var i = 0; i < text.Length; i++)
            DrawGlyph(tileX + i, tileY, color, text[i]);
    }
    
    public void DrawBorder(int tileX, int tileY, int width, int height, Color4 color, float thickness = 0.15f)
    {
        DrawRect(tileX, tileY, width, thickness, color);
        DrawRect(tileX, tileY + height - thickness, width, thickness, color);

        DrawRect(tileX, tileY + thickness, thickness, height - 2 * thickness, color);
        DrawRect(tileX + width - thickness, tileY + thickness, thickness, height - thickness * 2, color);
    }
    
    public void PushClip(UiRect clip)
    {
        _clips.Push(_clips.Count == 0 ? clip : _clips.Peek().Intersect(clip));
    }

    public void PopClip()
    {
        if (_clips.Count == 0)
            throw new InvalidOperationException("No UI clip to pop.");

        _clips.Pop();
    }
    
    private bool IsClipped(int x, int y) =>
        _clips.Count > 0 && !_clips.Peek().Contains(x, y);

}
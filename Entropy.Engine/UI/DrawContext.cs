using Entropy.Engine.Rendering;
using OpenTK.Mathematics;

namespace Entropy.Engine.UI;

public class DrawContext
{
    public required QuadBatcher Batcher { get; init; }
    public required GlyphAtlas Atlas { get; init; }
    
    public Color4 DefaultForeground { get; init; } = Color4.White;

    public void DrawGlyph(int tileX, int tileY, Color4 color, char glyph) =>
        Batcher.AddTexturedQuad(tileX, tileY, 1, 1, color, glyph);
    public void DrawGlyph(float tileX, float tileY, Color4 color, char glyph) =>
        Batcher.AddTexturedQuad(tileX, tileY, 1, 1, color, glyph);
    public void DrawRect(int tileX, int tileY, int width, int height, Color4 color) =>
        Batcher.AddRect(tileX, tileY, width, height, color);
    public void DrawRect(float tileX, float tileY, float width, float height, Color4 color)
        => Batcher.AddRect(tileX, tileY, width, height, color);
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
}
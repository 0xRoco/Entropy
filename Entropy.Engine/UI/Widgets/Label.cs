using OpenTK.Mathematics;

namespace Entropy.Engine.UI.Widgets;

public class Label : Widget
{
    public string Text { get; set; } = "";
    public Color4 Color { get; set; } = UiTheme.Text;

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        var x = X + offsetX;
        var y = Y + offsetY;
        
        var length = Math.Min(Text.Length, Width);
        for (var i = 0; i < length; i++)
        {
            context.DrawGlyph(x + i, y, Color, Text[i]);
        }
    }
}
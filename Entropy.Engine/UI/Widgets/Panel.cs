using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Engine.UI.Widgets;

public class Panel : Widget
{
    public Color4 BorderColor { get; set; } = Color4.Gray;
    public Color4 BackgroundColor { get; set; } = new(0.08f, 0.08f, 0.12f, 0.85f);
    
    public bool DrawBackground { get; set; } = true;

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        var x = X + offsetX;
        var y = Y + offsetY;
        
        if (DrawBackground)
            context.DrawRect(x, y, Width, Height, BackgroundColor);
        
        context.DrawBorder(x, y, Width, Height, BorderColor);
        
        base.Draw(context, offsetX + X, offsetY + Y);
    }

    public override bool OnKey(Keys Key)
    {
        if (Key == Keys.Escape)
        {
            RequestClose();
            return true;
        }
        return base.OnKey(Key);
    }
}
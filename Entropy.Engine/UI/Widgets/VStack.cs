namespace Entropy.Engine.UI.Widgets;

public class VStack : Widget
{
    public int Spacing { get; set; } = 1;

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        var y = 0;
        foreach (var child in Children)
        {
            if (!child.Visible) continue;
            child.X = 0;
            child.Width = Width;
            child.Y = y;
            child.Draw(context, offsetX + X, offsetY + Y);
            y += child.Height + Spacing;
        }
    }
}
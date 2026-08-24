using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Engine.UI.Widgets;

public class ScrollView : Widget
{
    public Widget? Content { get; private set; }

    public int ScrollY { get; private set; }

    public ScrollView()
    {
        Focusable = true;
    }

    public void SetContent(Widget content)
    {
        if (Content != null)
            Remove(Content);

        Content = content;
        Add(content);
        ClampScroll();
    }

    public override bool OnKey(Keys key)
    {
        var maxScroll = MaxScroll;

        switch (key)
        {
            case Keys.Up:
                ScrollY = Math.Max(0, ScrollY - 1);
                return true;

            case Keys.Down:
                ScrollY = Math.Min(maxScroll, ScrollY + 1);
                return true;

            case Keys.PageUp:
                ScrollY = Math.Max(0, ScrollY - Height);
                return true;

            case Keys.PageDown:
                ScrollY = Math.Min(maxScroll, ScrollY + Height);
                return true;
        }

        return false;
    }

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        if (Content == null || Width <= 0 || Height <= 0)
            return;

        var x = X + offsetX;
        var y = Y + offsetY;

        context.PushClip(new UiRect(x, y, Width, Height));

        // Content starts at the top of the scrollable area and is shifted up.
        Content.Draw(context, x, y - ScrollY);

        context.PopClip();
    }

    private int ContentHeight =>
        Content switch
        {
            TextBlock text => text.LineCount,
            _ => Content?.Height ?? 0
        };

    private int MaxScroll => Math.Max(0, ContentHeight - Height);

    private void ClampScroll()
    {
        ScrollY = Math.Clamp(ScrollY, 0, MaxScroll);
    }
}
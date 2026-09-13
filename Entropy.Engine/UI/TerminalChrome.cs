using OpenTK.Mathematics;

namespace Entropy.Engine.UI;

public static class TerminalChrome
{
    public static void Window(DrawContext context, UiRect rect, string? title = null)
    {
        context.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, UiTheme.PanelBackground);
        context.DrawBorder(rect.X, rect.Y, rect.Width, rect.Height, UiTheme.PanelBorder);

        if (!string.IsNullOrWhiteSpace(title))
            Header(context, rect.X + 2, rect.Y + 1, title);
    }

    public static void Header(DrawContext context, int x, int y, string title)
    {
        context.DrawText(x, y, title.ToUpperInvariant(), UiTheme.Heading);
    }

    public static void Divider(DrawContext context, int x, int y, int width, Color4? color = null)
    {
        context.DrawRect(x, y, width, 0.08f, color ?? UiTheme.TextDim);
    }

    public static void SelectionRow(DrawContext context, int x, int y, int width, string text, bool selected, Color4? textColor = null)
    {
        if (selected)
            context.DrawRect(x, y, width, 1, UiTheme.Selection);

        context.DrawText(x + 1, y, text, selected ? UiTheme.TextBright : textColor ?? UiTheme.Text);
    }

    public static void Footer(DrawContext context, int x, int y, int width, params (char key, string label)[] hints)
    {
        Divider(context, x, y, width);
        var cursor = x + 1;
        foreach (var (key, label) in hints)
        {
            context.DrawText(cursor, y + 1, $"[{key}]", UiTheme.Keybind);
            context.DrawText(cursor + 3, y + 1, label, UiTheme.TextDim);
            cursor += label.Length + 5;
        }
    }
}

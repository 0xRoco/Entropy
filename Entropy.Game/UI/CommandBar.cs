using Entropy.Engine.UI;

namespace Entropy.Game.UI;

public class CommandBar : Widget
{
    public List<(char Key, string Label)> Hints { get; } = [];

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        var x = X + offsetX;
        var y = Y + offsetY;
        var maxX = x + Width;
        foreach (var (key, label) in Hints)
        {
            var w = (key != '\0' ? 3 : 0) + label.Length + 2;
            if (Width > 0 && x + w > maxX)
                break;

            if (key != '\0')
            {
                context.DrawText(x, y, $"[{key}]", UiTheme.Keybind);
                x += 3;
            }
            context.DrawText(x, y, label, UiTheme.TextDim);
            x += label.Length + 2;
        }

        base.Draw(context, offsetX, offsetY);
    }

    public void AddText(string text)
    {
        Hints.Add(('\0', text));
    }
}
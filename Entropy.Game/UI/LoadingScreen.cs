using Entropy.Engine.UI;
using OpenTK.Mathematics;

namespace Entropy.Game.UI;

public class LoadingScreen
{
    private const int PanelWidth = 36;
    private readonly List<(string Name, bool Done)> _entries;

    public LoadingScreen(IReadOnlyList<string> names) =>
        _entries = [.. names.Select(name => (name, false))];

    public void Complete(string name)
    {
        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Name != name)
                continue;

            _entries[i] = (name, true);
            return;
        }
    }

    public void Draw(DrawContext context, Vector2i viewportTiles)
    {
        var height = _entries.Count + 10;
        var x = Math.Max(1, (viewportTiles.X - PanelWidth) / 2);
        var y = Math.Max(1, (viewportTiles.Y - height) / 2);

        context.DrawRect(x, y, PanelWidth, height, UiTheme.PanelBackground);
        context.DrawBorder(x, y, PanelWidth, height, UiTheme.PanelBorder);
        context.DrawText(x + 2, y + 1, "Loading...", UiTheme.Keybind);

        for (var i = 0; i < _entries.Count; i++)
        {
            var (name, done) = _entries[i];
            var rowY = y + 3 + i;
            var current = !done && (i == 0 || _entries[i - 1].Done);

            if (current)
                context.DrawRect(x + 1, rowY, PanelWidth - 2, 1, UiTheme.Selection);

            context.DrawText(x + 2, rowY, name, done ? UiTheme.Valid : UiTheme.TextBright);

            if (done)
                context.DrawText(x + PanelWidth - 7, rowY, "done", UiTheme.Valid);
        }
    }
}
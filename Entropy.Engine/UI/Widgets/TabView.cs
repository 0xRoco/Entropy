using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Engine.UI.Widgets;

public class TabView : Widget
{
    public int SelectedIndex { get; private set; }

    public Widget ActiveFocusTarget =>
        _tabs.Count == 0
            ? this
            : _tabs[SelectedIndex].FocusTarget;
    
    private readonly List<Tab> _tabs = [];

    public void AddTab(string label, Widget content, Widget? focusTarget = null)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Tabs require a label.", nameof(label));

        if (content.Parent != null)
            throw new InvalidOperationException("Tab content must not already have a parent.");

        var isFirst = _tabs.Count == 0;

        content.Visible = isFirst;
        Add(content);

        _tabs.Add(new Tab(label, content, focusTarget ?? content));
    }

    public override bool OnKey(Keys key)
    {
        if (_tabs.Count == 0)
            return false;

        switch (key)
        {
            case Keys.Left:
                Select((SelectedIndex - 1 + _tabs.Count) % _tabs.Count);
                return true;

            case Keys.Right:
            case Keys.Tab:
                Select((SelectedIndex + 1) % _tabs.Count);
                return true;
        }

        return false;
    }

    public override void Draw(DrawContext context, int offsetX, int offsetY)
    {
        var x = X + offsetX;
        var y = Y + offsetY;
        var right = x + Width;
        var cursor = x;

        for (var i = 0; i < _tabs.Count && cursor < right; i++)
        {
            var tab = _tabs[i];
            var selected = i == SelectedIndex;
            var tabWidth = Math.Min(tab.Label.Length + 2, right - cursor);

            if (tabWidth <= 0)
                break;

            if (selected)
                context.DrawRect(cursor, y, tabWidth, 1, UiTheme.Selection);

            var maxLabelLength = Math.Max(0, tabWidth - 2);
            var label = tab.Label[..Math.Min(tab.Label.Length, maxLabelLength)];

            context.DrawText(
                cursor + 1,
                y,
                label,
                selected ? UiTheme.TextBright : UiTheme.TextDim);

            cursor += tabWidth;
        }

        if (_tabs.Count == 0)
            return;

        var active = _tabs[SelectedIndex].Content;

        if (active.Visible)
            active.Draw(context, x, y + 1);
    }

    private void Select(int index)
    {
        if (index < 0 || index >= _tabs.Count || index == SelectedIndex)
            return;

        _tabs[SelectedIndex].Content.Visible = false;
        SelectedIndex = index;
        _tabs[SelectedIndex].Content.Visible = true;

        Ui?.SetFocus(_tabs[SelectedIndex].FocusTarget);
    }

    private sealed record Tab(string Label, Widget Content, Widget FocusTarget);
}
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Engine.UI.Widgets;

public class ContextMenu : Panel
{
    public bool IsOpen => _ui.IsModal(this);
    
    private readonly Ui _ui;
    private readonly Label _title;
    private readonly ListView _list;
    private readonly List<(char Hotkey, Action Run)> _items = [];
    
    public ContextMenu(Ui ui)
    {
        _ui = ui;
        Closable = true;
        Anchor = UiAnchor.Absolute;
        _title = new Label { X = 1, Y = 1, Width = 18, Color = UiTheme.Info };
        Add(_title);
        _list = new ListView { X = 1, Y = 2, Width = 18, Height = 1 };
        Add(_list);
        _list.OnActivate += ActivateAt;
        CloseRequested += Hide;
    }

    public void Show(int x, int y, string title, IReadOnlyList<(string Label, Action Run)> items)
    {
        _items.Clear();
        _list.Items.Clear();

        var width = title.Length + 4;
        for (var i = 0; i < items.Count; i++)
        {
            var hotkey = (char)('a' + i);
            var label = $"({hotkey}) {items[i].Label}";
            _items.Add((hotkey, items[i].Run));
            _list.Items.Add(label);
            width = Math.Max(width, label.Length + 4);
        }

        _title.Text = title;
        _title.Width = width - 2;
        _list.Width = width - 2;
        _list.Height = Math.Max(1, items.Count);
        Width = width;
        Height = items.Count + 3;
        X = Math.Clamp(x, 0, Math.Max(0, _ui.ViewportTiles.X - Width));
        Y = Math.Clamp(y, 0, Math.Max(0, _ui.ViewportTiles.Y - Height));
        _list.SelectedIndex = 0;
        Visible = true;
        _ui.PushModal(this, _list);
    }

    public void Hide()
    {
        if (!IsOpen)
            return;

        _ui.PopModal(this);
        Visible = false;
    }

    public bool HandleKey(Keys key)
    {
        if (!IsOpen)
            return false;

        if (key == Keys.Escape)
        {
            Hide();
            return true;
        }

        var value = (int)key;
        if (value is >= 65 and <= 90)
        {
            var hotkey = char.ToLower((char)value);
            foreach (var item in _items)
            {
                if (item.Hotkey != hotkey)
                    continue;

                Hide();
                item.Run();
                return true;
            }
        }

        return _ui.HandleKey(key);
    }

    private void ActivateAt(int index)
    {
        if (index < 0 || index >= _items.Count)
            return;

        var run = _items[index].Run;
        Hide();
        run();
    }
}

using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.UI;

public sealed class PauseMenu
{
    public event Action? ResumeRequested;
    public event Action? SaveRequested;
    public event Action? MainMenuRequested;
    public event Action? QuitRequested;

    private readonly Ui _ui;
    private readonly Panel _panel;
    private readonly ListView _list;
    private readonly MenuEntry[] _entries;

    public PauseMenu(Vector2i viewportTiles)
    {
        _ui = new Ui { ViewportTiles = viewportTiles };
        _panel = new Panel
        {
            Width = 30,
            Height = 9,
            Anchor = Widget.UiAnchor.Center,
            Closable = false
        };
        _list = new ListView { X = 1, Y = 2, Width = 28, Height = 5 };
        _entries =
        [
            new MenuEntry("Resume", () => ResumeRequested?.Invoke()),
            new MenuEntry("Save Game", () => SaveRequested?.Invoke()),
            new MenuEntry("Main Menu", () => MainMenuRequested?.Invoke()),
            new MenuEntry("Quit", () => QuitRequested?.Invoke())
        ];

        _panel.Add(new Label { X = 1, Y = 1, Width = 28, Text = "PAUSED", Color = UiTheme.Keybind });
        _panel.Add(_list);
        _ui.AddRoot(_panel);

        foreach (var entry in _entries)
            _list.Items.Add(entry.Label);

        _list.OnActivate += ActivateSelection;
        _ui.SetFocus(_list);
    }

    public bool HandleKey(Keys key)
    {
        if (key == Keys.Escape)
        {
            ResumeRequested?.Invoke();
            return true;
        }

        return _ui.HandleKey(key);
    }

    public void Draw(DrawContext context) => _ui.Draw(context);

    public void Resize(Vector2i viewportTiles) => _ui.ViewportTiles = viewportTiles;

    private void ActivateSelection(int index)
    {
        if (index >= 0 && index < _entries.Length)
            _entries[index].Action();
    }

    private sealed record MenuEntry(string Label, Action Action);
}
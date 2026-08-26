using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.UI;

public class MainMenuScreen
{
     private readonly Ui _ui;
    private readonly Label _title;
    private readonly Label _subtitle;
    private readonly Panel _menuPanel;
    private readonly ListView _menuList;
    private readonly Panel _descriptionPanel;
    private readonly Label _descriptionLabel;

    private readonly List<MenuEntry> _entries;

    public event Action? NewGameRequested;
    public event Action? ExitRequested;

    public MainMenuScreen(Vector2i viewportTiles)
    {
        _ui = new Ui { ViewportTiles = viewportTiles };

        _title = new Label { Text = "E N T R O P Y", Color = UiTheme.Info };
        
        _subtitle = new Label { Text = "ASCII SURVIVAL ROGUELIKE", Color = UiTheme.TextDim };

        _menuPanel = new Panel { Width = 30, Height = 7, Anchor = Widget.UiAnchor.Center, MarginX = 8, MarginY = 6 };

        _menuList = new ListView { X = 1, Y = 1, Width = 28, Height = 5 };

        _entries =
        [
            new MenuEntry(
                "New Game",
                "Start a new survival run with a generated world.",
                () => NewGameRequested?.Invoke()),

            new MenuEntry(
                "Load Game",
                "Loading saved games is not implemented yet.",
                null),

            new MenuEntry(
                "Settings",
                "Settings are not implemented yet.",
                null),

            new MenuEntry(
                "Help",
                "View controls and interface help during gameplay.",
                null),

            new MenuEntry(
                "Quit",
                "Exit Entropy.",
                () => ExitRequested?.Invoke())
        ];

        foreach (var entry in _entries)
            _menuList.Items.Add(entry.Label);

        _menuList.OnActivate += ActivateSelection;
        _menuPanel.Add(_menuList);

        _descriptionPanel = new Panel
        {
            Height = 3,
            Anchor = Widget.UiAnchor.BottomLeft,
            MarginX = 1,
            MarginY = 1
        };

        _descriptionLabel = new Label
        {
            X = 1,
            Y = 1,
            Color = UiTheme.Keybind
        };

        _descriptionPanel.Add(_descriptionLabel);

        _ui.AddRoot(_title);
        _ui.AddRoot(_subtitle);
        _ui.AddRoot(_menuPanel);
        _ui.AddRoot(_descriptionPanel);

        Reflow(viewportTiles);
        _ui.SetFocus(_menuList);
    }

    public bool HandleKey(Keys key)
    {
        if (key == Keys.N)
        {
            NewGameRequested?.Invoke();
            return true;
        }

        if (key == Keys.Q || key == Keys.Escape)
        {
            ExitRequested?.Invoke();
            return true;
        }

        return _ui.HandleKey(key);
    }

    public void Draw(DrawContext context)
    {
        UpdateDescription();
        _ui.Draw(context);
    }

    public void Resize(Vector2i viewportTiles) => Reflow(viewportTiles);

    private void ActivateSelection(int index)
    {
        if (index < 0 || index >= _entries.Count)
            return;

        var entry = _entries[index];

        if (entry.Action == null)
            return;

        entry.Action();
    }

    private void UpdateDescription()
    {
        if (_menuList.SelectedIndex < 0 || _menuList.SelectedIndex >= _entries.Count)
            return;

        _descriptionLabel.Text = _entries[_menuList.SelectedIndex].Description;
        _descriptionLabel.Width = Math.Max(0, _descriptionPanel.Width - 2);
    }

    private void Reflow(Vector2i viewportTiles)
    {
        _ui.ViewportTiles = viewportTiles;

        _title.Anchor = Widget.UiAnchor.Absolute;
        _title.Width = _title.Text.Length;
        _title.X = Math.Max(0, (viewportTiles.X - _title.Width) / 2);
        _title.Y = 5;

        _subtitle.Anchor = Widget.UiAnchor.Absolute;
        _subtitle.Width = _subtitle.Text.Length;
        _subtitle.X = Math.Max(0, (viewportTiles.X - _subtitle.Width) / 2);
        _subtitle.Y = 7;

        _descriptionPanel.Width = Math.Max(1, viewportTiles.X - 2);
        _descriptionLabel.Width = Math.Max(0, _descriptionPanel.Width - 2);
    }

    private sealed record MenuEntry(string Label, string Description, Action? Action);
}
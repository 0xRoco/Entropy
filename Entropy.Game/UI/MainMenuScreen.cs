using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using Entropy.Game.Systems;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.UI;

public class MainMenuScreen
{
    public event Action? CharacterCreationRequested;
    public event Action? LoadGameRequested;
    public event Action? ExitRequested;

    private static readonly string[] Logo =
    [
        "##### #   # ##### ####   ###  ####  #   #",
        "#     #   #   #   #   # #   # #   #  # # ",
        "###   ##  #   #   ##### #   # ####    #  ",
        "#     #  ##   #   #  #  #   # #       #  ",
        "##### #   #   #   #   #  ###  #       #  ",
    ];

    private static readonly string[] Tips =
    [
        "Tip: E examines the tile you are facing.",
        "Tip: right-click anything for its available actions.",
        "Tip: hostile creatures attack when you walk into them.",
        "Tip: sleeping in a bed restores fatigue, if nothing wakes you.",
        "Tip: stores restock never. Buy early, loot early.",
        "Tip: find the key before you find the lock it opens.",
        "Tip: G picks up items on your tile; E reaches one tile ahead.",
        "Tip: F5 reloads content while playing.",
    ];

    private readonly Ui _ui;
    private readonly Label[] _logoLabels;
    private readonly Label _versionLabel;
    private readonly Panel _menuPanel;
    private readonly ListView _menuList;
    private readonly CommandBar _hotkeyStrip;
    private readonly Label _descriptionLabel;
    private readonly Label _tipLabel;

    private readonly List<MenuEntry> _entries;
    private int _viewportWidth;

    public MainMenuScreen(Vector2i viewportTiles)
    {
        _ui = new Ui { ViewportTiles = viewportTiles };

        _logoLabels =
        [
            .. Logo.Select((row, i) =>
            {
                var label = new Label
                {
                    Text = row,
                    Width = row.Length,
                    Color = i < 3 ? UiTheme.Valid : UiTheme.Info
                };
                _ui.AddRoot(label);
                return label;
            })
        ];

        _versionLabel = new Label
        {
            Text = "Version 0.1 - The Descent",
            Color = UiTheme.TextDim
        };
        _ui.AddRoot(_versionLabel);

        _menuPanel = new Panel { Width = 26, Height = 7, Closable = false };
        _menuList = new ListView { X = 1, Y = 1, Width = 24, Height = 5 };

        _entries =
        [
            new MenuEntry(
                "New Game",
                "Create a character and enter the neighborhood.",
                () => CharacterCreationRequested?.Invoke()),

            new MenuEntry(
                "Load Game",
                "Continue the last run.",
                () => LoadGameRequested?.Invoke()),

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
        _ui.AddRoot(_menuPanel);

        _hotkeyStrip = new CommandBar { Anchor = Widget.UiAnchor.Absolute };
        _hotkeyStrip.Hints.Add(('N', "ew Game"));
        _hotkeyStrip.Hints.Add(('L', "oad"));
        _hotkeyStrip.Hints.Add(('S', "ettings"));
        _hotkeyStrip.Hints.Add(('H', "elp"));
        _hotkeyStrip.Hints.Add(('Q', "uit"));
        _ui.AddRoot(_hotkeyStrip);

        _descriptionLabel = new Label
        {
            Anchor = Widget.UiAnchor.Absolute,
            Color = UiTheme.Text
        };
        _ui.AddRoot(_descriptionLabel);

        _tipLabel = new Label
        {
            Anchor = Widget.UiAnchor.Absolute,
            Color = UiTheme.Info,
            Text = Tips[Random.Shared.Next(Tips.Length)]
        };
        _ui.AddRoot(_tipLabel);

        Reflow(viewportTiles);
        _ui.SetFocus(_menuList);
    }

    public bool HandleKey(Keys key)
    {
        switch (key)
        {
            case Keys.N:
                CharacterCreationRequested?.Invoke();
                return true;
            case Keys.L when GameSave.Exists:
                LoadGameRequested?.Invoke();
                return true;
            case Keys.Q or Keys.Escape:
                ExitRequested?.Invoke();
                return true;
            default:
                return _ui.HandleKey(key);
        }
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

        _entries[index].Action?.Invoke();
    }

    private void UpdateDescription()
    {
        if (_menuList.SelectedIndex < 0 || _menuList.SelectedIndex >= _entries.Count)
            return;

        _descriptionLabel.Text = _entries[_menuList.SelectedIndex].Description;
        CenterLabel(_descriptionLabel);
    }

    private void CenterLabel(Label label)
    {
        label.Width = label.Text.Length;
        label.X = Math.Max(0, (_viewportWidth - label.Width) / 2);
    }

    private void Reflow(Vector2i viewportTiles)
    {
        _ui.ViewportTiles = viewportTiles;

        var logoX = Math.Max(0, (viewportTiles.X - Logo[0].Length) / 2);
        for (var i = 0; i < _logoLabels.Length; i++)
        {
            _logoLabels[i].Anchor = Widget.UiAnchor.Absolute;
            _logoLabels[i].X = logoX;
            _logoLabels[i].Y = 2 + i;
        }

        _versionLabel.Anchor = Widget.UiAnchor.Absolute;
        _versionLabel.Width = _versionLabel.Text.Length;
        _versionLabel.X = Math.Max(0, (viewportTiles.X - _versionLabel.Width) / 2);
        _versionLabel.Y = 8;

        _menuPanel.Anchor = Widget.UiAnchor.Center;

        _hotkeyStrip.Width = viewportTiles.X;
        _viewportWidth = viewportTiles.X;

        var stripWidth = _hotkeyStrip.Hints.Sum(hint =>
            (hint.Key != '\0' ? 3 : 0) + hint.Label.Length + 2);

        _hotkeyStrip.X = Math.Max(0, (viewportTiles.X - stripWidth) / 2);
        _hotkeyStrip.Y = viewportTiles.Y - 5;

        CenterLabel(_descriptionLabel);
        _descriptionLabel.Y = viewportTiles.Y - 4;

        CenterLabel(_tipLabel);
        _tipLabel.Y = viewportTiles.Y - 3;
    }

    private sealed record MenuEntry(string Label, string Description, Action? Action);
}

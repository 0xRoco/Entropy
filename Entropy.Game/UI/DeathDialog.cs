using Entropy.Engine.Core;
using Entropy.Engine.ECS;
using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.UI;

public class DeathDialog
{
    private readonly Ui _ui;
    private readonly Panel _panel;
    private readonly ListView _list;
    private readonly Label _epitaph;

    private readonly World _world;
    private readonly Entity _player;
    private readonly WorldClock _clock;
    private readonly Func<int> _seed;

    public event Action? NewCharacterRequested;
    public event Action? MainMenuRequested;

    public bool IsOpen => _ui.IsModal(_panel);

    public DeathDialog(Ui ui, World world, Entity player, WorldClock clock, Func<int> seed)
    {
        _ui = ui;
        _world = world;
        _player = player;
        _seed = seed;
        _clock = clock;

        _panel = new Panel { Width = 44, Height = 10, Anchor = Widget.UiAnchor.Center, Closable = false, Visible = false };

        var title = new Label { X = 1, Y = 1, Width = 42, Text = "You died.", Color = UiTheme.Danger };
        _panel.Add(title);

        _epitaph = new Label { X = 1, Y = 3, Width = 42, Color = UiTheme.TextDim,
            Text = $"Died {clock.Date()}, {clock.Time()}.  Seed: {_seed()}."
        };
        _panel.Add(_epitaph);

        _list = new ListView { X = 1, Y = 5, Width = 42, Height = 2 };
        _list.Items.Add("New Character");
        _list.Items.Add("Main Menu");
        _list.OnActivate += ActivateSelection;
        _panel.Add(_list);
    }

    public void Open()
    {
        if (IsOpen) return;

        _epitaph.Text = $"Survived {_clock.TotalMinutes} turns.  Seed: {_seed()}.";
        _list.SelectedIndex = 0;

        _panel.Visible = true;
        _ui.PushModal(_panel, _list);
    }

    public void Close()
    {
        if (!IsOpen) return;

        _panel.Visible = false;
        _ui.PopModal(_panel);
    }

    public bool HandleKey(Keys key)
    {
        if (!IsOpen) return false;
        return _ui.HandleKey(key); 
    }

    private void ActivateSelection(int index)
    {
        switch (index)
        {
            case 0:
                Close();
                NewCharacterRequested?.Invoke();
                break;
            case 1:
                Close();
                MainMenuRequested?.Invoke();
                break;
        }
    }
}
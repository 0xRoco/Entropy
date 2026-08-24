using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.UI;

public class HelpDialog
{
    private readonly Ui _ui;
    private readonly Panel _panel;
    private readonly ScrollView _scroll;

    public bool IsOpen { get; private set; }

    public HelpDialog(Ui ui)
    {
        _ui = ui;

        _panel = new Panel
        {
            Width = 42,
            Height = 18,
            Anchor = Widget.UiAnchor.Center,
            Closable = true,
            Visible = false
        };

        var text = new TextBlock
        {
            X = 0,
            Y = 0,
            Width = 38,
            Text = """
                   Help

                   Arrow keys move the player. Press G to pick up items
                   on your current tile. Press I to open inventory.

                   In inventory, press Enter to open an item action menu.
                   The available actions depend on the item itself. Weapons
                   can be wielded, medical supplies can be used, and all
                   carried items can be dropped or examined.

                   Zombies detect by sight and smell. Break line of sight
                   around a wall to make them investigate your last known
                   position instead of tracking you directly.

                   The status sidebar shows current health, wielded weapon,
                   turn count, and the world seed.

                   This text wraps and scrolls inside the dialog. Use the
                   arrow keys or Page Up and Page Down to read more.
                   """
        };

        _scroll = new ScrollView
        {
            X = 1,
            Y = 1,
            Width = 40,
            Height = 16
        };

        _scroll.SetContent(text);
        _panel.Add(_scroll);

        _panel.CloseRequested += Close;
    }

    public void Open()
    {
        if (IsOpen) return;

        IsOpen = true;
        _panel.Visible = true;
        _ui.AddRoot(_panel);
        _ui.SetFocus(_scroll);
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        _panel.Visible = false;
        _ui.RemoveRoot(_panel);
        _ui.SetFocus(null);
    }

    public bool HandleKey(Keys key)
    {
        if (!IsOpen) return false;

        if (key == Keys.Escape || key == Keys.Slash)
        {
            Close();
            return true;
        }

        _ui.HandleKey(key);
        return true;
    }
}
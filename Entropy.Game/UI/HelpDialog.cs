using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.UI;

public class HelpDialog
{
private readonly Ui _ui;
    private readonly Panel _panel;
    private readonly TabView _tabs;

    public bool IsOpen => _ui.IsModal(_panel);

    public HelpDialog(Ui ui)
    {
        _ui = ui;

        _panel = new Panel
        {
            Width = 58,
            Height = 24,
            Anchor = Widget.UiAnchor.Center,
            Closable = true,
            Visible = false
        };

        _tabs = new TabView
        {
            X = 1,
            Y = 1,
            Width = 56,
            Height = 22
        };

        var controls = CreatePage(
            """
            Movement

            Arrow keys move the player one tile at a time.

            G picks up every item on your current tile. I opens your
            inventory. Press Enter on an inventory item to open its
            available actions.

            Camera

            Hold W, A, S, or D to pan the camera. Use the mouse wheel
            to change zoom. Click inside the map viewport to examine a tile.

            General

            Press ? to open this help screen. Press Escape to close menus
            or return to the previous dialog. F6 saves the current game.
            """);

        var inventory = CreatePage(
            """
            Inventory

            Press I to open inventory. Use Up and Down to select an item.

            Press Enter to open that item's context menu. Available actions
            depend on the item: weapons can be wielded, medical supplies can
            be used, and carried items can be dropped or examined.

            A wielded item is marked with cyan text and "(wielded)".
            """);

        var survival = CreatePage(
            """
            Survival

            Zombies detect creatures through sight and smell. Breaking line
            of sight behind terrain makes them search the last place they
            saw you instead of directly tracking your current position.

            Your health is shown in the sidebar. The health bar changes from
            green to yellow to red as it drops.
            """);

        _tabs.AddTab("Controls", controls, controls);
        _tabs.AddTab("Inventory", inventory, inventory);
        _tabs.AddTab("Survival", survival, survival);

        _panel.Add(_tabs);
        _panel.CloseRequested += Close;
    }

    public void Open()
    {
        if (IsOpen)
            return;

        _panel.Visible = true;
        _ui.PushModal(_panel, _tabs.ActiveFocusTarget);
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        _panel.Visible = false;
        _ui.PopModal(_panel);
    }

    public bool HandleKey(Keys key)
    {
        if (!IsOpen)
            return false;

        if (key is Keys.Escape or Keys.Slash)
        {
            Close();
            return true;
        }

        return _ui.HandleKey(key);
    }

    private static ScrollView CreatePage(string text)
    {
        var textBlock = new TextBlock
        {
            X = 1,
            Y = 0,
            Width = 54,
            Text = text
        };

        var scroll = new ScrollView
        {
            X = 0,
            Y = 0,
            Width = 56,
            Height = 21
        };

        scroll.SetContent(textBlock);
        return scroll;
    }
}

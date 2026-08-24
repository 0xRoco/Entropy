using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.UI;

public class GameHud
{
    private readonly Ui _ui;
    private readonly StatusPanel _statusPanel;
    private readonly MessageLogPanel _logPanel;
    private readonly Panel _commandPanel;
    private readonly Label _commandLabel;
    private readonly InventoryDialog _inventoryDialog;
    private readonly HelpDialog _helpDialog;

    public GameplayLayout Layout { get; private set; }

    public GameHud(GameContext context, Func<int> turnCount, int seed, Vector2i viewportTiles)
    {
        _ui = new Ui { ViewportTiles = viewportTiles };

        _statusPanel = new StatusPanel(context.World, context.Player, turnCount, seed)
        {
            Width = 26
        };

        _logPanel = new MessageLogPanel
        {
            Log = context.Log
        };

        _commandPanel = new Panel
        {
            DrawBackground = true
        };

        _commandLabel = new Label
        {
            Text = "[g]et  [i]nventory  [arrows] move  [.] wait  [?] help",
            Color = Color4.LightGray
        };

        _commandPanel.Add(_commandLabel);

        _ui.AddRoot(_commandPanel);
        _ui.AddRoot(_logPanel);
        _ui.AddRoot(_statusPanel);

        _inventoryDialog = new InventoryDialog(_ui, context);
        _helpDialog = new HelpDialog(_ui);

        Reflow(viewportTiles);
    }

    public bool HandleKey(Keys key)
    {
        if (_helpDialog.IsOpen)
            return _helpDialog.HandleKey(key);

        if (_inventoryDialog.IsOpen)
            return _inventoryDialog.HandleKey(key);

        if (key == Keys.I)
        {
            _inventoryDialog.Open();
            return true;
        }

        if (key == Keys.Slash)
        {
            _helpDialog.Open();
            return true;
        }

        return false;
    }

    public void Draw(DrawContext context) => _ui.Draw(context);

    public void Resize(Vector2i viewportTiles) => Reflow(viewportTiles);

    private void Reflow(Vector2i viewportTiles)
    {
        _ui.ViewportTiles = viewportTiles;
        Layout = GameplayLayout.Create(viewportTiles);

        ApplyBounds(_statusPanel, Layout.Sidebar);
        ApplyBounds(_logPanel, Layout.Messages);
        ApplyBounds(_commandPanel, Layout.Commands);

        _commandLabel.X = 1;
        _commandLabel.Y = 1;
        _commandLabel.Width = Math.Max(0, Layout.Commands.Width - 2);
    }

    private static void ApplyBounds(Widget widget, UiRect bounds)
    {
        widget.Anchor = Widget.UiAnchor.Absolute;
        widget.X = bounds.X;
        widget.Y = bounds.Y;
        widget.Width = bounds.Width;
        widget.Height = bounds.Height;
    }
}
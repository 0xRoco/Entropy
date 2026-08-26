using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using Entropy.Game.Components;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.UI;

public class GameHud
{
    
    public GameplayLayout Layout { get; private set; }
    public event Action? NewCharacterRequested;
    public event Action? MainMenuRequested;

    
    private readonly Ui _ui;
    private readonly DeathDialog _deathDialog;
    private readonly StatusPanel _statusPanel;
    private readonly MessageLogPanel _logPanel;
    private readonly Panel _commandPanel;
    private readonly InventoryDialog _inventoryDialog;
    private readonly HelpDialog _helpDialog;
    private readonly CommandBar _commandBar;
    private readonly GameContext _context;
    
    public GameHud(GameContext context, Func<int> turnCount, int seed, Vector2i viewportTiles)
    {
        _context = context;
        _ui = new Ui { ViewportTiles = viewportTiles };
        _statusPanel = new StatusPanel(context.World, context.Player, turnCount, seed) { Width = 26};

        _logPanel = new MessageLogPanel
        {
            Log = context.Log
        };
        
        _commandBar = new CommandBar();
        _commandBar.AddText("arrows move");
        _commandBar.Hints.Add(('g', "et item"));
        _commandBar.Hints.Add(('i', "nventory"));
        _commandBar.Hints.Add(('?', "Help"));

        _commandPanel = new Panel
        {
            DrawBackground = true
        };
        

        _commandPanel.Add(_commandBar);

        _ui.AddRoot(_commandPanel);
        _ui.AddRoot(_logPanel);
        _ui.AddRoot(_statusPanel);

        _inventoryDialog = new InventoryDialog(_ui, context);
        _helpDialog = new HelpDialog(_ui);
        
        _deathDialog = new DeathDialog(_ui, context.World, context.Player, turnCount, () => seed);
        _deathDialog.NewCharacterRequested += OnNewCharacterRequested;
        _deathDialog.MainMenuRequested += OnMainMenuRequested;

        Reflow(viewportTiles);
    }

    public bool HandleKey(Keys key)
    {
        if (_helpDialog.IsOpen)
            return _helpDialog.HandleKey(key);

        if (_inventoryDialog.IsOpen)
            return _inventoryDialog.HandleKey(key);
        
        if (_deathDialog.IsOpen)
            return _deathDialog.HandleKey(key);

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

    public void Draw(DrawContext drawContext)
    {
        if (_context.World.IsAlive(_context.Player)
            && _context.World.Has<Health>(_context.Player)
            && _context.World.Get<Health>(_context.Player).Current <= 0
            && !_deathDialog.IsOpen
            && !_helpDialog.IsOpen)
        {
            _deathDialog.Open();
        }
        
        _ui.Draw(drawContext);
    }

    public void Resize(Vector2i viewportTiles) => Reflow(viewportTiles);

    private void Reflow(Vector2i viewportTiles)
    {
        _ui.ViewportTiles = viewportTiles;
        Layout = GameplayLayout.Create(viewportTiles);

        ApplyBounds(_statusPanel, Layout.Sidebar);
        ApplyBounds(_logPanel, Layout.Messages);
        ApplyBounds(_commandPanel, Layout.Commands);

        _commandBar.X = 1;
        _commandBar.Y = 1;
        _commandBar.Width = Math.Max(0, Layout.Commands.Width - 2);
    }

    private static void ApplyBounds(Widget widget, UiRect bounds)
    {
        widget.Anchor = Widget.UiAnchor.Absolute;
        widget.X = bounds.X;
        widget.Y = bounds.Y;
        widget.Width = bounds.Width;
        widget.Height = bounds.Height;
    }
    
    private void OnNewCharacterRequested() => NewCharacterRequested?.Invoke();
    private void OnMainMenuRequested() => MainMenuRequested?.Invoke();
}
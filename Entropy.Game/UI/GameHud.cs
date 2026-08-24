using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using Entropy.Game.Components;
using Entropy.Game.Systems;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.UI;

public class GameHud
{
    public Ui Ui { get; }
    public bool InventoryOpen { get; private set; }

    private readonly List<Entity> _rowEntities = [];
    private readonly GameContext _context;
    private readonly Entity _player;

    private readonly Panel _inventoryPanel;
    private readonly ListView _inventoryList;
    private readonly MessageLogPanel _logPanel;
    private readonly StatusPanel _statusPanel;

    private readonly Panel _contextMenuPanel;
    private readonly ListView _contextMenuList;
    private List<ItemAction> _currentActions = [];
    private Entity _contextMenuItem;
    private bool _contextMenuOpen;

    public GameHud(GameContext context, Func<int> turnCount, int seed, Vector2i viewportTiles)
    {
        _context = context;
        _player = context.Player;
        Ui = new Ui { ViewportTiles = viewportTiles };

        _statusPanel = new StatusPanel(context.World, _player, turnCount, seed)
        {
            Width = 24,
            Height = viewportTiles.Y,
            Anchor = Widget.UiAnchor.BottomRight,
        };
        
        _logPanel = new MessageLogPanel
        {
            Log = context.Log,
            Width = viewportTiles.X - Math.Max(1, _statusPanel.Width) - 2,
            Anchor = Widget.UiAnchor.BottomLeft,
            MarginX = 1,
            MarginY = 1
        };

        Ui.AddRoot(_logPanel);
        Ui.AddRoot(_statusPanel);

        _inventoryPanel = new Panel { X = 1, Y = 1, Width = 30, Height = 12, Closable = true };
        _inventoryPanel.Add(new Label
            { X = 1, Y = 1, Width = 28, Text = "Inventory", Color = Color4.Yellow });

        _inventoryList = new ListView { X = 1, Y = 3, Width = 28, Height = 8, ItemColor = RowColor };
        _inventoryPanel.Add(_inventoryList);

        _inventoryList.OnActivate += OpenMenuFor;

        _inventoryPanel.CloseRequested += CloseInventory;

        _contextMenuPanel = new Panel { Width = 16, Closable = true };
        _contextMenuList = new ListView { X = 1, Y = 1, Width = 12, Height = 6 };
        _contextMenuPanel.Add(_contextMenuList);
        _contextMenuList.OnActivate += i =>
        {
            if (i >= 0 && i < _currentActions.Count)
                ExecuteAction(_currentActions[i]);
        };
        _contextMenuPanel.CloseRequested += CloseContextMenu;
    }

    public void ToggleInventory()
    {
        if (InventoryOpen) CloseInventory();
        else OpenInventory();
    }

    public bool HandleKey(Keys key)
    {
        if (key == Keys.I)
        {
            if (_contextMenuOpen) { CloseContextMenu(); return true; }
            ToggleInventory();
            return true;
        }

        if (!_contextMenuOpen) return Ui.HandleKey(key) || InventoryOpen;
        
        var ch = KeyToChar(key);
        if (ch != null)
        {
            var action = _currentActions.FirstOrDefault(a => a.Hotkey == ch);
            if (action != null) { ExecuteAction(action); return true; }
        }

        if (key != Keys.Escape) return Ui.HandleKey(key) || InventoryOpen;
        CloseContextMenu(); return true;

    }

    public void Draw(DrawContext context) => Ui.Draw(context);

    public void Resize(Vector2i viewportTiles)
    {
        Ui.ViewportTiles = viewportTiles;
        _statusPanel.Height = viewportTiles.Y;
        _logPanel.Width = viewportTiles.X - Math.Max(1, _statusPanel.Width) - 2;
    }

    private void OpenInventory()
    {
        if (InventoryOpen) return;
        InventoryOpen = true;
        RefreshInventory();
        Ui.AddRoot(_inventoryPanel);
        Ui.SetFocus(_inventoryList);
    }

    private void CloseInventory()
    {
        if (!InventoryOpen) return;
        InventoryOpen = false;
        if (_contextMenuOpen) CloseContextMenu();
        Ui.RemoveRoot(_inventoryPanel);
        Ui.SetFocus(null);
    }

    private void OpenMenuFor(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= _rowEntities.Count) return;

        var row = _inventoryList.VisibleRowOf(itemIndex);
        if (row == null) return;

        var item = _rowEntities[itemIndex];
        _contextMenuItem = item;
        _currentActions = ItemActions.AvailableFor(_context, item);

        _contextMenuList.Items.Clear();
        foreach (var a in _currentActions)
            _contextMenuList.Items.Add($"({char.ToUpper(a.Hotkey)}) {a.Label}");
        _contextMenuList.SelectedIndex = 0;

        _contextMenuPanel.X = _inventoryPanel.X + _inventoryPanel.Width + 1;
        _contextMenuPanel.Y = _inventoryPanel.Y + 3 + row.Value;
        _contextMenuPanel.Height = _currentActions.Count + 2;

        Ui.AddRoot(_contextMenuPanel);
        Ui.SetFocus(_contextMenuList);
        _contextMenuOpen = true;
    }

    private void CloseContextMenu()
    {
        if (!_contextMenuOpen) return;
        _contextMenuOpen = false;
        Ui.RemoveRoot(_contextMenuPanel);
        Ui.SetFocus(_inventoryList); 
    }

    private void ExecuteAction(ItemAction action)
    {
        action.Execute(_context, _player, _contextMenuItem);
        CloseContextMenu();
        RefreshInventory();
    }

    private void RefreshInventory()
    {
        _inventoryList.Items.Clear();
        _rowEntities.Clear();

        Entity? wielded = null;
        if (_context.World.Has<Equipped>(_player))
        {
            var w = _context.World.Get<Equipped>(_player).Item;
            if (_context.World.IsAlive(w)) wielded = w;
        }

        foreach (var item in ItemSystem.GetItems(_context.World, _player))
        {
            if (!_context.World.IsAlive(item)) continue;
            var identity = _context.World.Get<ItemIdentity>(item);
            var glyph = _context.World.Get<Glyph>(item);
            var text = identity.Name;
            if (_context.World.Has<Stackable>(item))
            {
                var stack = _context.World.Get<Stackable>(item);
                text += $" x{stack.Count}";
            }
            if (wielded != null && item.Equals(wielded))
                text += " (wielded)";

            _inventoryList.Items.Add($"[{glyph.Character}] {text}");
            _rowEntities.Add(item);
        }
    }

    private Color4 RowColor(int index)
    {
        if (index < 0 || index >= _rowEntities.Count || !_context.World.Has<Equipped>(_player)) return Color4.White;

        var wielded = _context.World.Get<Equipped>(_player).Item;
        return _rowEntities[index].Equals(wielded) ? Color4.Cyan : Color4.White;
    }

    private static char? KeyToChar(Keys key)
    {
        var c = (int)key;
        // GLFW: A=65 - Z=90
        if (c is >= 65 and <= 90) return char.ToLower((char)c);
        return null;
    }
}
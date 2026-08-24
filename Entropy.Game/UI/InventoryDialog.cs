using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using Entropy.Game.Components;
using Entropy.Game.Systems;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.UI;

public class InventoryDialog
{
    private readonly Ui _ui;
    private readonly GameContext _context;
    private readonly Entity _player;
    private readonly List<Entity> _rowEntities = [];

    private readonly Panel _panel;
    private readonly ListView _list;
    private readonly Panel _contextMenuPanel;
    private readonly ListView _contextMenuList;

    private List<ItemAction> _currentActions = [];
    private Entity _contextMenuItem;
    private bool _contextMenuOpen;

    public bool IsOpen { get; private set; }

    public InventoryDialog(Ui ui, GameContext context)
    {
        _ui = ui;
        _context = context;
        _player = context.Player;

        _panel = new Panel
        {
            X = 1,
            Y = 1,
            Width = 30,
            Height = 12,
            Anchor = Widget.UiAnchor.Absolute,
            Closable = true
        };

        _panel.Add(new Label
        {
            X = 1,
            Y = 1,
            Width = 28,
            Text = "Inventory",
            Color = Color4.Yellow
        });

        _list = new ListView
        {
            X = 1,
            Y = 3,
            Width = 28,
            Height = 8,
            ItemColor = RowColor
        };

        _panel.Add(_list);
        _list.OnActivate += OpenContextMenuFor;
        _panel.CloseRequested += Close;

        _contextMenuPanel = new Panel
        {
            Width = 16,
            Anchor = Widget.UiAnchor.Absolute,
            Closable = true
        };

        _contextMenuList = new ListView
        {
            X = 1,
            Y = 1,
            Width = 14,
            Height = 6
        };

        _contextMenuPanel.Add(_contextMenuList);

        _contextMenuList.OnActivate += index =>
        {
            if (index >= 0 && index < _currentActions.Count)
                ExecuteAction(_currentActions[index]);
        };

        _contextMenuPanel.CloseRequested += CloseContextMenu;
    }

    public void Open()
    {
        if (IsOpen) return;

        IsOpen = true;
        Refresh();
        _ui.AddRoot(_panel);
        _ui.SetFocus(_list);
    }

    public void Close()
    {
        if (!IsOpen) return;

        if (_contextMenuOpen)
            CloseContextMenu();

        IsOpen = false;
        _ui.RemoveRoot(_panel);
        _ui.SetFocus(null);
    }

    public bool HandleKey(Keys key)
    {
        if (!IsOpen) return false;

        if (key == Keys.I)
        {
            Close();
            return true;
        }

        if (_contextMenuOpen)
            return HandleContextMenuKey(key);

        if (key == Keys.Escape)
        {
            Close();
            return true;
        }

        _ui.HandleKey(key);
        return true; // inventory is modal: swallow unhandled gameplay keys
    }

    private bool HandleContextMenuKey(Keys key)
    {
        if (key == Keys.Escape)
        {
            CloseContextMenu();
            return true;
        }

        var ch = KeyToChar(key);
        if (ch != null)
        {
            var action = _currentActions.FirstOrDefault(a => a.Hotkey == ch.Value);
            if (action != null)
            {
                ExecuteAction(action);
                return true;
            }
        }

        _ui.HandleKey(key);
        return true; // context menu is modal
    }

    private void OpenContextMenuFor(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= _rowEntities.Count)
            return;

        var row = _list.VisibleRowOf(itemIndex);
        if (row == null)
            return;

        _contextMenuItem = _rowEntities[itemIndex];
        _currentActions = ItemActions.AvailableFor(_context, _contextMenuItem);

        _contextMenuList.Items.Clear();

        foreach (var action in _currentActions)
            _contextMenuList.Items.Add($"({char.ToUpper(action.Hotkey)}) {action.Label}");

        _contextMenuList.SelectedIndex = 0;

        _contextMenuPanel.X = _panel.X + _panel.Width + 1;
        _contextMenuPanel.Y = _panel.Y + 3 + row.Value;
        _contextMenuPanel.Height = _currentActions.Count + 2;

        _ui.AddRoot(_contextMenuPanel);
        _ui.SetFocus(_contextMenuList);
        _contextMenuOpen = true;
    }

    private void CloseContextMenu()
    {
        if (!_contextMenuOpen) return;

        _contextMenuOpen = false;
        _ui.RemoveRoot(_contextMenuPanel);
        _ui.SetFocus(_list);
    }

    private void ExecuteAction(ItemAction action)
    {
        action.Execute(_context, _player, _contextMenuItem);
        CloseContextMenu();
        Refresh();
    }

    private void Refresh()
    {
        _list.Items.Clear();
        _rowEntities.Clear();

        Entity? wielded = null;

        if (_context.World.Has<Equipped>(_player))
        {
            var item = _context.World.Get<Equipped>(_player).Item;
            if (_context.World.IsAlive(item))
                wielded = item;
        }

        foreach (var item in ItemSystem.GetItems(_context.World, _player))
        {
            if (!_context.World.IsAlive(item))
                continue;

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

            _list.Items.Add($"[{glyph.Character}] {text}");
            _rowEntities.Add(item);
        }
    }

    private Color4 RowColor(int index)
    {
        if (index < 0 || index >= _rowEntities.Count || !_context.World.Has<Equipped>(_player))
            return Color4.White;

        var wielded = _context.World.Get<Equipped>(_player).Item;
        return _rowEntities[index].Equals(wielded) ? Color4.Cyan : Color4.White;
    }

    private static char? KeyToChar(Keys key)
    {
        var value = (int)key;
        return value is >= 65 and <= 90
            ? char.ToLower((char)value)
            : null;
    }
}
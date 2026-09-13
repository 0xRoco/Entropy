using Entropy.Engine.ECS;
using Entropy.Engine.ECS.Components;
using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using Entropy.Game.Components;
using Entropy.Game.Components.Identity;
using Entropy.Game.Components.Inventory;
using Entropy.Game.Systems;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.UI;

public class InventoryDialog
{
    public bool IsOpen => _ui.IsModal(_panel);
    
    private readonly Ui _ui;
    private readonly GameContext _context;
    private readonly Entity _player;
    private readonly List<Entity> _rowEntities = [];

    private readonly Panel _panel;
    private readonly ListView _list;
    private readonly ContextMenu _contextMenu;

    private List<ItemAction> _currentActions = [];
    private Entity _contextMenuItem;
    private bool IsContextMenuOpen => _contextMenu.IsOpen;
    public InventoryDialog(Ui ui, GameContext context)
    {
        _ui = ui;
        _context = context;
        _player = context.Player;

        _panel = new Panel { Anchor = Widget.UiAnchor.Absolute, Closable = true };

        _panel.Add(new Label { X = 1, Y = 1, Text = "INVENTORY", Color = UiTheme.Heading });

        _list = new ListView { X = 1, Y = 3, ItemColor = RowColor };

        _panel.Add(_list);
        _list.OnActivate += OpenContextMenuFor;
        _panel.CloseRequested += Close;

        _contextMenu = new ContextMenu(ui);
    }

    public void Resize(Vector2i viewportTiles)
    {
        _panel.Width = Math.Clamp(viewportTiles.X - 12, 40, 96);
        _panel.Height = Math.Clamp(viewportTiles.Y - 8, 16, 32);
        _panel.X = Math.Max(1, (viewportTiles.X - _panel.Width) / 2);
        _panel.Y = Math.Max(1, (viewportTiles.Y - _panel.Height) / 2);
        _list.Width = _panel.Width - 2;
        _list.Height = _panel.Height - 5;
        _panel.Children[0].Width = _panel.Width - 2;
    }

    public void Open()
    {
        if (IsOpen)
            return;

        Refresh();
        _ui.PushModal(_panel, _list);
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        if (IsContextMenuOpen)
            _contextMenu.Hide();

        _ui.PopModal(_panel);
    }

    public bool HandleKey(Keys key)
    {
        if (!IsOpen)
            return false;

        if (IsContextMenuOpen)
            return _contextMenu.HandleKey(key);

        if (key is not (Keys.I or Keys.Escape)) return _ui.HandleKey(key);
        Close();
        return true;

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

        var actions = _currentActions
            .Select(action => (action.Label, Run: (Action)(() => ExecuteAction(action))))
            .ToList();

        var title = _context.World.Get<ItemIdentity>(_contextMenuItem).Name;
        var menuX = _panel.X + _panel.Width + 1;
        if (menuX + title.Length + actions.Count + 8 > _ui.ViewportTiles.X)
            menuX = Math.Max(1, _panel.X - title.Length - 14);
        _contextMenu.Show(menuX, _panel.Y + 3 + row.Value, title, actions);
    }

    private void ExecuteAction(ItemAction action)
    {
        action.Execute(_context, _player, _contextMenuItem);
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
            return UiTheme.Text;

        var wielded = _context.World.Get<Equipped>(_player).Item;
        return _rowEntities[index].Equals(wielded) ? UiTheme.Info : UiTheme.Text;
    }
}

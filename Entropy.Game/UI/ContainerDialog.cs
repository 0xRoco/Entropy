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

public class ContainerDialog
{
    public bool IsOpen => _ui.IsModal(_panel);
    public event Action<ActionResult>? ActionCompleted;

    private readonly Ui _ui;
    private readonly Panel _panel;
    private readonly Label _title;
    private readonly Label _hint;
    private readonly ListView _list;
    private readonly ContextMenu _contextMenu;
    private readonly List<Entity> _rows = [];

    private GameContext? _context;
    private Entity _player;
    private Entity _container;
    private bool _showingInventory;

    public ContainerDialog(Ui ui, GameContext context)
    {
        _ui = ui;
        _context = context;
        _player = context.Player;

        _panel = new Panel
            { X = 1, Y = 1, Width = 32, Height = 15, Anchor = Widget.UiAnchor.Absolute, Closable = true };
        _title = new Label { X = 1, Y = 1, Width = 30, Color = UiTheme.Keybind };
        _hint = new Label { X = 1, Y = 2, Width = 30, Color = UiTheme.Text };
        _list = new ListView { X = 1, Y = 4, Width = 30, Height = 10 };

        _panel.Add(_title);
        _panel.Add(_hint);
        _panel.Add(_list);
        _list.OnActivate += OpenContextMenuFor;
        _panel.CloseRequested += Close;

        _contextMenu = new ContextMenu(ui);
    }

    public void Open(GameContext context, Entity player, Entity container)
    {
        if (IsOpen)
            return;

        _context = context;
        _player = player;
        _container = container;
        _showingInventory = false;

        Refresh();
        _ui.PushModal(_panel, _list);
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        if (_contextMenu.IsOpen)
            _contextMenu.Hide();

        _ui.PopModal(_panel);
    }

    public bool HandleKey(Keys key)
    {
        if (!IsOpen)
            return false;

        if (_contextMenu.IsOpen)
            return _contextMenu.HandleKey(key);

        switch (key)
        {
            case Keys.Escape or Keys.I:
                Close();
                return true;
            case Keys.Tab:
                SwitchView(!_showingInventory);
                return true;
            default:
                return _ui.HandleKey(key);
        }
    }

    private void SwitchView(bool inventory)
    {
        _showingInventory = inventory;
        Refresh();
    }

    private void OpenContextMenuFor(int index)
    {
        if (index < 0 || index >= _rows.Count)
            return;
        if (_context is null)
            return;

        var item = _rows[index];
        var row = _list.VisibleRowOf(index);
        if (row is null)
            return;

        var name = _context.World.Get<ItemIdentity>(item).Name;
        var actions = new List<(string Label, Action Run)>();

        if (_showingInventory)
        {
            actions.Add(("Put here", () => Put(item, name)));
            actions.Add(("Examine", () => Examine(item)));
        }
        else
        {
            if (PurchaseSystem.IsShopStock(_context, _container))
            {
                actions.Add(("Buy", () => Buy(item, name)));
                actions.Add(("Steal", () => Steal(item, name)));
            }
            else
            {
                actions.Add(("Take", () => Take(item, name)));
            }
            actions.Add(("Examine", () => Examine(item)));
        }

        _contextMenu.Show(_panel.X + _panel.Width + 1, _panel.Y + 3 + row.Value, name, actions);
    }

    private void Take(Entity item, string name)
    {
        if (_context is null)
            return;

        if (ItemSystem.Transfer(_context.World, item, _player))
        {
            _context.Log.Add($"You take the {name}.");
            ActionCompleted?.Invoke(ActionResult.Turn);
        }

        Refresh();
    }

    private void Put(Entity item, string name)
    {
        if (_context is null)
            return;

        if (ItemSystem.Transfer(_context.World, item, _container))
        {
            _context.Log.Add($"You put the {name} away.");
            ActionCompleted?.Invoke(ActionResult.Turn);
        }

        Refresh();
    }

    private void Buy(Entity item, string name)
    {
        if (_context is null)
            return;

        if (PurchaseSystem.TryPurchase(_context, _player, item, _container))
            ActionCompleted?.Invoke(ActionResult.Turn);

        Refresh();
    }

    private void Steal(Entity item, string name)
    {
        if (_context is null)
            return;

        if (PurchaseSystem.TrySteal(_context, _player, item, _container))
            ActionCompleted?.Invoke(ActionResult.Turn);

        Refresh();
    }

    private void Examine(Entity item)
    {
        if (_context is null)
            return;

        var identity = _context.World.Get<ItemIdentity>(item);
        var def = _context.Definitions.Item(identity.DefinitionId);
        _context.Log.Add($"{identity.Name}: {def.Description}", Color4.LightGray);
    }

    private void Refresh()
    {
        if (_context is null)
            return;

        _rows.Clear();
        _list.Items.Clear();

        var containerName = _context.World.Has<WorldObjectIdentity>(_container)
            ? _context.World.Get<WorldObjectIdentity>(_container).Name
            : "Container";

        if (_showingInventory)
        {
            _title.Text = $"Your pack - put into {containerName}";
            _hint.Text = "(tab) contents · (esc) close";
        }
        else
        {
            _title.Text = $"{containerName} contents";
            _hint.Text = "(tab) your pack · (esc) close";
        }

        var source = _showingInventory
            ? ItemSystem.GetItems(_context.World, _player)
            : ItemSystem.GetItems(_context.World, _container);

        foreach (var item in source)
        {
            if (!_context.World.IsAlive(item))
                continue;

            var identity = _context.World.Get<ItemIdentity>(item);
            var glyph = _context.World.Get<Glyph>(item);
            var text = identity.Name;

            if (_context.World.Has<Stackable>(item))
                text += $" x{_context.World.Get<Stackable>(item).Count}";

            if (!_showingInventory && PurchaseSystem.IsShopStock(_context, _container))
            {
                var definitionId = _context.World.Get<ItemIdentity>(item).DefinitionId;
                var price = _context.Definitions.Item(definitionId).PriceCents;
                text += price > 0 ? $" ${price / 100}.{price % 100:00}" : " (not for sale)";
            }

            _list.Items.Add($"[{glyph.Character}] {text}");
            _rows.Add(item);
        }

        _list.SelectedIndex = 0;
    }
}

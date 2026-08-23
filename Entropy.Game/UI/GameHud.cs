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
    public bool InventoryOpen { get; private set;}
    public Action<int>? OnItemDropped;
    public Action<int>? OnItemActivated;
    public Action<int>? OnItemWielded;
    
    private readonly List<Entity> _rowEntities = [];
    private readonly World _world;
    private readonly Entity _player;
    private readonly Panel _inventoryPanel;
    private readonly ListView _inventoryList;
    private readonly MessageLogPanel _logPanel;

    public GameHud(MessageLog log, World world, Entity player, Vector2i viewportTiles)
    {
        _world = world;
        _player = player;
        Ui = new Ui { ViewportTiles = viewportTiles };
        _logPanel = new MessageLogPanel
        {
            Log = log,
            Width = 50,
            Anchor = Widget.UiAnchor.BottomLeft,
            MarginX = 1,
            MarginY = 1
        };
        Ui.AddRoot(_logPanel);

        _inventoryPanel = new Panel { X = 1, Y = 1, Width = 30, Height = 10, Closable = true};
        _inventoryPanel.Add(new Label { X = 1, Y = 1, Width = 30, Text = "Inventory", Color = Color4.Yellow }); 
        
        _inventoryList = new ListView { X = 1, Y = 4, Width = 30, Height = 3,
            ItemColor = RowColor
        };
        _inventoryPanel.Add(_inventoryList);
        
        _inventoryList.OnActivate += i =>
        {
            OnItemActivated?.Invoke(i);
            RefreshInventory();
        };
        
        _inventoryList.OnDrop += i =>
        {
            OnItemDropped?.Invoke(i);
            RefreshInventory();
        };
        
        _inventoryList.OnWield += i =>
        {
            OnItemWielded?.Invoke(i);
            RefreshInventory();
        };

        _inventoryPanel.CloseRequested += ToggleInventory;
        _inventoryList.OnActivate += OnInventoryActivate;

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
            ToggleInventory();
            return true;
        }

        if (Ui.HandleKey(key)) return true;

        return InventoryOpen;
    }

    public void Draw(DrawContext context) => Ui.Draw(context);
    public void Resize(Vector2i viewportTiles) => Ui.ViewportTiles = viewportTiles;

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
        Ui.RemoveRoot(_inventoryPanel);
        Ui.SetFocus(null);
    }
    
    private void RefreshInventory()
    {
        _inventoryList.Items.Clear();
        _rowEntities.Clear();

        Entity? wielded = null;
        if (_world.Has<Equipped>(_player))
        {
            var w = _world.Get<Equipped>(_player).Item;
            if (_world.IsAlive(w)) wielded = w;
        }

        foreach (var item in ItemSystem.GetItems(_world, _player))
        {
            if (!_world.IsAlive(item)) continue;
            var identity = _world.Get<ItemIdentity>(item);
            var glyph = _world.Get<Glyph>(item);
            var text = identity.Name;
            if (_world.Has<Stackable>(item))
            {
                var stack = _world.Get<Stackable>(item);
                text += $" x{stack.Count}";
            }
            if (wielded != null && item.Equals(wielded))
                text += " (wielded)";

            _inventoryList.Items.Add($"[{glyph.Character}] {text}");
            _rowEntities.Add(item);
        }
    }
    
    private void OnInventoryActivate(int index)
    {
        RefreshInventory();
    }
    
    private Color4 RowColor(int index)
    {
        if (index < 0 || index >= _rowEntities.Count) return Color4.White;

        if (_world.Has<Equipped>(_player))
        {
            var wielded = _world.Get<Equipped>(_player).Item;
            if (_rowEntities[index].Equals(wielded)) return Color4.Cyan;
        }
        return Color4.White;
    }
}
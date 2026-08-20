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
        
        _inventoryList = new ListView { X = 1, Y = 4, Width = 30, Height = 3 };
        _inventoryPanel.Add(_inventoryList);
        
        _inventoryList.OnActivate += i => log.Add($"You fiddle with the {_inventoryList.Items[i]}.", Color4.Cyan);
        
        
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
        foreach (var item in ItemSystem.GetItems(_world, _player))
        {
            if (!_world.IsAlive(item)) continue;
            var identity = _world.Get<ItemIdentity>(item);
            var glyph = _world.Get<Glyph>(item);
            _inventoryList.Items.Add($"[{glyph.Character}] {identity.Name}");
        }
    }
    
    private void OnInventoryActivate(int index)
    {
        RefreshInventory();
    }

}
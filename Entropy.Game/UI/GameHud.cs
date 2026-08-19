using Entropy.Engine.UI;
using Entropy.Engine.UI.Widgets;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Game.UI;

public class GameHud
{
    public Ui Ui { get; }
    public bool InventoryOpen { get; private set;}

    private readonly Panel _inventoryPanel;
    private readonly ListView _inventoryList;
    private MessageLogPanel _logPanel;

    public GameHud(MessageLog log, Vector2i viewportTiles)
    {
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

        _inventoryPanel = new Panel
        { X = 5, Y = 3, Width = 30, Height = 10, Closable = true};
        _inventoryPanel.Add(new Label
        { X = 1, Y = 1, Width = 28, Text = "Inventory", Color = Color4.Yellow }); 
        
        _inventoryList = new ListView { X = 1, Y = 3, Width = 28, Height = 3 };
        _inventoryList.Items.Add("Iron Sword");
        _inventoryList.Items.Add("Bandage");
        _inventoryList.Items.Add("Crackers");
        _inventoryList.Items.Add("Wrench");
        _inventoryList.Items.Add("Flashlight");
        _inventoryList.Items.Add("Empty Bottle");
        _inventoryPanel.Add(_inventoryList);
        
        _inventoryList.OnActivate += i => log.Add($"You fiddle with the {_inventoryList.Items[i]}.", Color4.Cyan);
        
        
        _inventoryPanel.CloseRequested += ToggleInventory;

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
}
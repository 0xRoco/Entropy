using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Engine.UI.Widgets;

public class ListView : Widget
{
    public List<string> Items { get; set; } = [];
    public int SelectedIndex { get; set; } 
    public Color4 TextColor { get; set; } = Color4.White;
    public Action<int>? OnActivate { get; set; }
    public Action<int>? OnDrop { get; set; }
    public Action<int>? OnWield { get; set; }
    public Func<int, Color4>? ItemColor { get; set; }

    private int _scrollOffset;
    private static readonly Color4 SelectionBackground = new(0.25f, 0.25f, 0.35f, 0.9f);

    public ListView()
    {
        Focusable = true;
    }

    public override bool OnKey(Keys Key)
    {
        if (Items.Count == 0)
            return false;

        switch (Key)
        {
            case Keys.Up:
                SelectedIndex = Math.Max(0, SelectedIndex - 1);
                EnsureSelectionVisible();
                return true;
            case Keys.Down:
                SelectedIndex = Math.Min(Items.Count - 1, SelectedIndex + 1);
                EnsureSelectionVisible();
                return true;
            case Keys.Enter:
                OnActivate?.Invoke(SelectedIndex);
                return true;
            case Keys.D:
                OnDrop?.Invoke(SelectedIndex);
                return true;
            case Keys.W:
                OnWield?.Invoke(SelectedIndex);
                return true;
                
        }
        
        return false;
    }
    
    public override void Draw(DrawContext ctx, int offsetX, int offsetY)
    {
        var x = X + offsetX;
        var y = Y + offsetY;

        var visibleRows = Math.Max(0, Height);
        EnsureSelectionVisible();

        for (var row = 0; row < visibleRows && row + _scrollOffset < Items.Count; row++)
        {
            var itemIndex = _scrollOffset + row;
            var selected = itemIndex == SelectedIndex && Focused;
            var color = ItemColor?.Invoke(itemIndex) ?? TextColor;
            var text = Items[itemIndex];
            var length = Math.Min(text.Length, Width);
            
            if (selected)
                ctx.DrawRect(x, y + row, Math.Min(length + 2, Width), 1, SelectionBackground);
            
            for (var c = 0; c < length; c++)
                ctx.DrawGlyph(x + c, y + row, selected ? Color4.White : color, text[c]);
        }
    }
    
    private void EnsureSelectionVisible()
    {
        if (SelectedIndex < _scrollOffset)
            _scrollOffset = SelectedIndex;
        else if (SelectedIndex >= _scrollOffset + Height)
            _scrollOffset = SelectedIndex - Height + 1;
    }
}
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Engine.UI;

public abstract class Widget
{
    public int X { get; set; }
    public int Y { get; set; }
    public int MarginX { get; set; } 
    public int MarginY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Visible { get; set; } = true;
    public bool Focusable { get; protected set; } = false;
    public bool Closable { get;  set; } = false;
    public bool Focused => Ui?.Focused == this;
    public Ui? Ui { get; internal set; }
    public Widget? Parent { get; internal set; }
    public readonly List<Widget> Children = [];
    public event Action? CloseRequested;
    
    public UiAnchor Anchor { get; set; } = UiAnchor.TopLeft;
    public enum UiAnchor {Absolute, TopLeft, BottomLeft, BottomRight, TopRight, Center }

    public void Add(Widget child)
    {
        child.Parent = this; 
        Children.Add(child);
        if (Ui != null)
            PropagateUi(child);
    }
    
    public void Remove(Widget child)
    {
        if (Children.Remove(child))
            child.Parent = null;
    }
    
    public virtual bool OnKey(Keys Key) => false;

    public virtual void Draw(DrawContext context, int offsetX, int offsetY)
    {
        foreach (var child in Children)
        {
            if (child.Visible)
                child.Draw(context, offsetX + X, offsetY + Y);
        }
    }

    public (int AbsX, int AbsY) AbsolutePosition
    {
        get
        {
            var (px, py) = Parent?.AbsolutePosition ?? (0, 0);
            return (px + X, py + Y);
        }
    }

    protected void RequestClose()
    {
        if (!Closable) return;
        CloseRequested?.Invoke();
    }

    internal void SetParentInternal(Widget parent) => Parent = parent;
    public virtual void OnFocusGained() {}
    public virtual void OnFocusLost() {}

    private void PropagateUi(Widget child)
    {
        child.Ui = Ui;
        foreach (var grandChild in child.Children)
            PropagateUi(grandChild);
    }

}
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Engine.UI;

public abstract class Widget
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Visible { get; set; } = true;
    public bool Focusable { get; protected set; } = false;
    public Widget? Parent { get; internal set; }
    public readonly List<Widget> Children = [];

    public void Add(Widget child)
    {
        child.Parent = this; 
        Children.Add(child);
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

    internal void SetParentInteral(Widget parent) => Parent = parent;
    public virtual void OnFocusGained() {}
    public virtual void OnFocusLost() {}

}
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Engine.UI;

public class Ui
{ 
    public Widget? Focused { get; private set; }
    public Vector2i ViewportTiles { get; set; }
    
    private readonly List<Widget> _roots = [];
    
    public void AddRoot(Widget widget)
    {
        if (widget.Parent != null)
            throw new InvalidOperationException($"Root widget must not have a parent. Widget: {widget}");
        _roots.Add(widget);
        StampUi(widget);
    }

    public void RemoveRoot(Widget widget)
    { 
        if (_roots.Remove(widget))
        {
            StampUi(widget, true);
            SetFocus(null);
        }
    }

    public void SetFocus(Widget? widget)
    {
        if (Focused == widget)
            return;

        Focused?.OnFocusLost();
        Focused = widget;
        Focused?.OnFocusGained();
    }

    public bool HandleKey(Keys key)
    {
        var node = Focused;
        while (node != null)
        {
            if (node.OnKey(key)) return true;
            node = node.Parent;
        }

        return false;
    }

    public void Draw(DrawContext context)
    {
        foreach (var root in _roots)
        {
            if (!root.Visible) continue;
            ResolveAnchor(root);
            root.Draw(context, 0, 0);
        }
    }

    private void StampUi(Widget widget, bool clear = false)
    {
        widget.Ui = clear ? null : this;
        foreach (var child in widget.Children)
            StampUi(child, clear);
    }

    private void ResolveAnchor(Widget widget)
    {
        var vw = ViewportTiles.X;
        var vh = ViewportTiles.Y;
        switch (widget.Anchor)
        {
            case Widget.UiAnchor.TopLeft:
                widget.X = widget.MarginX;
                widget.Y = widget.MarginY;
                break;
            case Widget.UiAnchor.TopRight:
                widget.X = vw - widget.Width - widget.MarginX;
                widget.Y = widget.MarginY;
                break;
            case Widget.UiAnchor.BottomLeft:
                widget.X = widget.MarginX;
                widget.Y = vh - widget.Height - widget.MarginY;
                break;
            case Widget.UiAnchor.BottomRight:
                widget.X = vw - widget.Width - widget.MarginX;
                widget.Y = vh - widget.Height - widget.MarginY;
                break;
            case Widget.UiAnchor.Center:
                widget.X = (vw - widget.Width) / 2;
                widget.Y = (vh - widget.Height) / 2;
                break;
            case Widget.UiAnchor.Absolute:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
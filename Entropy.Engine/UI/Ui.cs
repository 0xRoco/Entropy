using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Engine.UI;

public class Ui
{ 
    public Widget? Focused { get; private set; }
    public Vector2i ViewportTiles { get; set; }
    
    public bool HasModal => _modals.Count > 0;
    public Widget? ActiveModal => _modals.Count > 0 ? _modals.Peek().Root : null;
    
    private readonly List<Widget> _roots = [];
    private readonly Stack<ModalEntry> _modals = [];
    
    public void AddRoot(Widget widget)
    {
        if (widget.Parent != null)
            throw new InvalidOperationException("Root widgets must not have a parent.");

        if (_roots.Contains(widget))
            return;

        _roots.Add(widget);
        StampUi(widget);
    }

    public void RemoveRoot(Widget widget)
    {
        if (IsModal(widget))
            throw new InvalidOperationException("Use PopModal to remove modal widgets.");

        if (!RemoveRootInternal(widget))
            return;

        if (IsDescendantOf(Focused, widget))
            SetFocus(null);
    }

    public void PushModal(Widget root, Widget focus)
    {
        if (root.Parent != null)
            throw new InvalidOperationException("Modal widgets must not have a parent.");

        if (IsModal(root))
            return;

        _modals.Push(new ModalEntry(root, Focused));
        AddRoot(root);
        SetFocus(focus);
    }

    public bool PopModal(Widget? expectedRoot = null)
    {
        if (_modals.Count == 0)
            return false;

        var modal = _modals.Peek();

        if (expectedRoot != null && !ReferenceEquals(modal.Root, expectedRoot))
            return false;

        _modals.Pop();
        RemoveRootInternal(modal.Root);
        SetFocus(modal.PreviousFocus);

        return true;
    }

    public bool IsModal(Widget widget) =>
        _modals.Any(modal => ReferenceEquals(modal.Root, widget));

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
            if (node.OnKey(key))
                return true;

            node = node.Parent;
        }

        // any unhandled key is still consumed while a modal is open.
        return HasModal;
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

    private bool RemoveRootInternal(Widget widget)
    {
        if (!_roots.Remove(widget))
            return false;
        
        StampUi(widget, true);
        return true;
    }

    private void StampUi(Widget widget, bool clear = false)
    {
        widget.Ui = clear ? null : this;
        foreach (var child in widget.Children)
            StampUi(child, clear);
    }
    
    private static bool IsDescendantOf(Widget? widget, Widget ancestor)
    {
        while (widget != null)
        {
            if (ReferenceEquals(widget, ancestor))
                return true;
            
            widget = widget.Parent;
        }
        
        return false;
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
    
    private sealed record ModalEntry(Widget Root, Widget? PreviousFocus);
}
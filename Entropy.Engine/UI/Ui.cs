using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Entropy.Engine.UI;

public class Ui
{
    private readonly List<Widget> _roots = [];
    public Widget? Focused { get; private set; }

    public void AddRoot(Widget widget)
    {
        if (widget.Parent != null)
            throw new InvalidOperationException($"Root widget must not have a parent. Widget: {widget}");
        _roots.Add(widget);
    }
    
    public void RemoveRoot(Widget widget) => _roots.Remove(widget);

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
            if (root.Visible)
                root.Draw(context, 0, 0);
        }
    }
}
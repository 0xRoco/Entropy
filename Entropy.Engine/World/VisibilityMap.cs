namespace Entropy.Engine.World;

public class VisibilityMap
{
    private readonly bool[] _visible;
    private readonly bool[] _explored;
    public int Width { get; }
    public int Height { get; }
    
    public VisibilityMap(int width, int height)
    {
        Width = width;
        Height = height;
        _visible = new bool[width * height];
        _explored = new bool[width * height];
    }
    
    public bool IsVisible(int x, int y) => _visible[y * Width + x];
    public bool IsExplored(int x, int y) => _explored[y * Width + x];

    public void SetVisible(int x, int y)
    {
        _visible[y * Width + x] = true;
        _explored[y * Width + x] = true;
    }

    public void ClearVisible() => Array.Clear(_visible, 0, _visible.Length);
    public int VisibleCount() => _visible.Count(v => v);
}
using OpenTK.Mathematics;

namespace Entropy.Engine.World;

public struct Tile(char glyph, Color4 foreground, Color4 background, bool walkable, bool opaque)
{
    public readonly char Glyph = glyph;
    public Color4 Foreground = foreground;
    public Color4 Background = background;
    public readonly bool Walkable = walkable;
    public readonly bool Opaque = opaque;
    
    public static readonly Tile Floor = new('.', Color4.White, 
        new Color4(0.08f, 0.08f, 0.12f, 1f), true, false);
    public static readonly Tile Wall = new Tile('#', Color4.LightGray, 
        Color4.Black, false, true);
}
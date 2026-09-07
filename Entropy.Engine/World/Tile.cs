using OpenTK.Mathematics;

namespace Entropy.Engine.World;

[Flags]
public enum TileFlags : byte
{
    None = 0,
    Road = 1,
    Outdoor = 2,
    Indoor = 4,
}

public struct Tile(char glyph, Color4 foreground, Color4 background, bool walkable, bool opaque,
    ushort terrainDefIndex = 0, TileFlags flags = TileFlags.None, ushort moveCost = 100)
{
    public readonly char Glyph = glyph;
    public Color4 Foreground = foreground;
    public Color4 Background = background;
    public readonly bool Walkable = walkable;
    public readonly bool Opaque = opaque;
    public readonly ushort TerrainDefIndex = terrainDefIndex;
    public readonly TileFlags Flags = flags;
    public readonly ushort MoveCost = moveCost;

    public static readonly Tile Floor = new('.', Color4.White,
        new Color4(0.08f, 0.08f, 0.12f, 1f), true, false);
    public static readonly Tile Wall = new('#', Color4.LightGray,
        Color4.Black, false, true);
}

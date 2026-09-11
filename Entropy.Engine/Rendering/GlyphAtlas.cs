using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;

namespace Entropy.Engine.Rendering;

public class GlyphAtlas : IDisposable
{
    public int Handle { get; }
    public int CellRows { get; }
    public int CellCols { get; }
    
    public int Width { get; }
    public int Height { get; }

    public GlyphAtlas(string path, int cellRows = 16, int cellCols = 16)
    {
        var image = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);

        CellRows = cellRows;
        CellCols = cellCols;
        Width = image.Width;
        Height = image.Height;

        Handle = Upload(image.Data, image.Width, image.Height);
    }

    protected GlyphAtlas(byte[] rgba, int width, int height, int cellRows, int cellCols)
    {
        CellRows = cellRows;
        CellCols = cellCols;
        Width = width;
        Height = height;

        Handle = Upload(rgba, width, height);
    }

    private static int Upload(byte[] data, int width, int height)
    {
        var handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, handle);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
            width, height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, data);

        return handle;
    }

    public (Vector2 tl, Vector2 tr, Vector2 br, Vector2 bl) GetUv(char c)
    {
        var row = c / CellCols;
        var col = c % CellCols;

        var uMin = col / (float)CellCols;
        var uMax = (col + 1) / (float)CellCols;

        var vMin = row / (float)CellRows;
        var vMax = (row + 1) / (float)CellRows;

        var insetU = 0.5f / Width;
        var insetV = 0.5f / Height;

        uMin += insetU;
        uMax -= insetU;
        vMin += insetV;
        vMax -= insetV;

        return (
            new Vector2(uMin, vMin),
            new Vector2(uMax, vMin),
            new Vector2(uMax, vMax),
            new Vector2(uMin, vMax)
        );
    }

    public void Bind()
    {
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }

    public Vector2 GetCellCenter(char c)
    {
        var row = c / CellCols;
        var col = c % CellCols;
        return new Vector2((col + 0.5f) / CellCols, (row + 0.5f) / CellRows);
    }
    
    public void Dispose()
    {
        GL.DeleteTexture(Handle);
    }
}
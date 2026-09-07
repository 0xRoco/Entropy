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
        CellRows = cellRows;
        CellCols = cellCols;
        
        var image = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);

        Handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, Handle);
        
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 
            image.Width, image.Height, 0, 
            PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
        
        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 1, 1,
            PixelFormat.Rgba, PixelType.UnsignedByte,
            new byte[] { 255, 255, 255, 255 });

        Width = image.Width;
        Height = image.Height;
    }

    public (Vector2 tl, Vector2 tr, Vector2 br, Vector2 bl) GetUv(char c)
    {
        var row = c / 16;
        var col = c % 16;

        var inset = 0.5f / (CellCols * 16f);

        var uMin = col / 16f;
        var uMax = (col + 1) / 16f;

        var vMin = row / 16f;
        var vMax = (row + 1) / 16f;
        
        uMin += inset;
        uMax -= inset;
        vMin += inset;
        vMax -= inset;

        var tl = new Vector2(uMin, vMin);
        var tr = new Vector2(uMax, vMin);
        var br = new Vector2(uMax, vMax);
        var bl = new Vector2(uMin, vMax);

        return (tl, tr, br, bl);
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
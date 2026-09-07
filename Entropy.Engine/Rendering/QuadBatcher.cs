using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Entropy.Engine.Rendering;

public class QuadBatcher : IDisposable
{
    private const int MaxQuads = 10000;
    private const int MaxVertices = MaxQuads * 4;
    private const int MaxIndices = MaxQuads * 6;
    
    private readonly Vertex[] _vertices = new Vertex[MaxVertices];
    private readonly ushort[] _indices = new ushort[MaxIndices];
    private int _vertexCount;
    
    private readonly int _vao, _vbo, _ebo;
    private readonly Shader _shader;
    private readonly Camera _camera;
    private readonly GlyphAtlas _atlas;


    public QuadBatcher(Shader shader, Camera camera, GlyphAtlas atlas)
    {
        _shader = shader;
        _camera = camera;
        _atlas = atlas;

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();
        
        GL.BindVertexArray(_vao);
        
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, MaxVertices * 32, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        
        GenerateIndices();
        
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, MaxIndices * sizeof(ushort), _indices, BufferUsageHint.StaticDraw);
        
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 32, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 32, 8);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 32, 24);

        GL.BindVertexArray(0);
    }

    public void AddTexturedQuad(float x, float y, float w, float h, Color4 color, char glyph)
    {
        if (_vertexCount + 4 > MaxVertices) Flush();

        var (tl, tr, br, bl) = _atlas.GetUv(glyph);
        _vertices[_vertexCount + 0] = new Vertex { Position = new Vector2(x, y),         Color = color, Uv = tl };
        _vertices[_vertexCount + 1] = new Vertex { Position = new Vector2(x + w, y),     Color = color, Uv = tr };
        _vertices[_vertexCount + 2] = new Vertex { Position = new Vector2(x + w, y + h), Color = color, Uv = br };
        _vertices[_vertexCount + 3] = new Vertex { Position = new Vector2(x, y + h),     Color = color, Uv = bl };
        _vertexCount += 4;
    }
    
    public void AddTexturedQuad(float x, float y, float w, float h, Color4 color,
        Vector2 uvMin, Vector2 uvMax)
    {
        if (_vertexCount + 4 > MaxVertices) Flush();

        _vertices[_vertexCount + 0] = new Vertex { Position = new Vector2(x, y),         Color = color, Uv = uvMin };
        _vertices[_vertexCount + 1] = new Vertex { Position = new Vector2(x + w, y),     Color = color, Uv = new Vector2(uvMax.X, uvMin.Y) };
        _vertices[_vertexCount + 2] = new Vertex { Position = new Vector2(x + w, y + h), Color = color, Uv = uvMax };
        _vertices[_vertexCount + 3] = new Vertex { Position = new Vector2(x, y + h),     Color = color, Uv = new Vector2(uvMin.X, uvMax.Y) };
        _vertexCount += 4;
    }

    public void AddRect(float x, float y, float w, float h, Color4 color)
    {
        if (_vertexCount + 4 > MaxVertices) Flush();

        var uv = _atlas.GetCellCenter((char)219); // Use a solid block character for the rectangle

        _vertices[_vertexCount + 0] = new Vertex { Position = new Vector2(x, y),         Color = color, Uv = uv };
        _vertices[_vertexCount + 1] = new Vertex { Position = new Vector2(x + w, y),     Color = color, Uv = uv };
        _vertices[_vertexCount + 2] = new Vertex { Position = new Vector2(x + w, y + h), Color = color, Uv = uv };
        _vertices[_vertexCount + 3] = new Vertex { Position = new Vector2(x, y + h),     Color = color, Uv = uv };
        _vertexCount += 4;
    }
    
    public void Flush()
    {
        if (_vertexCount == 0) return;
        _shader.Use();
        var projection = _camera.GetProjection();
        _shader.SetMatrix4("uProjection", ref projection);
        
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertexCount * 32, _vertices);
        
        GL.ActiveTexture(TextureUnit.Texture0);
        _atlas.Bind();
        _shader.SetInt("uAtlas", 0);
        
        GL.DrawElements(BeginMode.Triangles, _vertexCount / 4 * 6, DrawElementsType.UnsignedShort, 0);
        
        _vertexCount = 0;
    }
    
    
    private void GenerateIndices()
    {
        for (var i = 0; i < MaxQuads; i++)
        {
            var offset = i * 4;
            _indices[i * 6 + 0] = (ushort)(offset + 0);
            _indices[i * 6 + 1] = (ushort)(offset + 1);
            _indices[i * 6 + 2] = (ushort)(offset + 2);
            _indices[i * 6 + 3] = (ushort)(offset + 2);
            _indices[i * 6 + 4] = (ushort)(offset + 3);
            _indices[i * 6 + 5] = (ushort)(offset + 0);
        }
    }

    public void Dispose()
    {
        GL.DeleteBuffer(_vbo);
        GL.DeleteBuffer(_ebo);
        GL.DeleteVertexArray(_vao);
    }
}
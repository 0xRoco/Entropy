using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Entropy.Engine.Rendering;

public class Shader : IDisposable
{
    public int Handle { get; }
    private readonly Dictionary<string, int> _uniforms = new();
    
    public Shader(string vertSource, string fragSource)
    {
        var vs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vs, vertSource);
        GL.CompileShader(vs);
        
        GL.GetShader(vs, ShaderParameter.CompileStatus, out int vStatus);
        if (vStatus == 0)
            throw new Exception($"Vertex shader compilation failed: {GL.GetShaderInfoLog(vs)}");
        
        var fs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fs, fragSource);
        GL.CompileShader(fs);
        
        GL.GetShader(fs, ShaderParameter.CompileStatus, out int fStatus);
        if (fStatus == 0)
            throw new Exception($"Fragment shader compilation failed: {GL.GetShaderInfoLog(fs)}");
        
        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vs);
        GL.AttachShader(Handle, fs);
        GL.LinkProgram(Handle);
        
        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
            throw new Exception($"Shader program linking failed: {GL.GetProgramInfoLog(Handle)}");
        
        GL.DetachShader(Handle, vs);
        GL.DetachShader(Handle, fs);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);
    }

    public static Shader FromFiles(string vertPath, string fragPath)
    {
        try
        {
            Console.WriteLine($"Compiled {vertPath} + {fragPath}");
            return new Shader(File.ReadAllText(vertPath), File.ReadAllText(fragPath));
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load shader '{vertPath}' / '{fragPath}': {ex.Message}", ex);
        }
    }
    
    public void Use()
    {
        GL.UseProgram(Handle);
    }

    public int GetUniformLocation(string name)
    {
        if (_uniforms.TryGetValue(name, out int location))
            return location;

        location = GL.GetUniformLocation(Handle, name);

        _uniforms[name] = location;
        return location;
    }

    public void SetMatrix4(string name, ref Matrix4 matrix)
    {
        var location = GetUniformLocation(name);
        if (location == -1) return;
        GL.UniformMatrix4(location, false, ref matrix);
    }

    public void SetInt(string name, int value)
    {
        var location = GetUniformLocation(name);
        if (location == -1) return;
        GL.Uniform1(location, value);
    }

    public void Dispose()
    {
        GL.DeleteProgram(Handle);
    }
}
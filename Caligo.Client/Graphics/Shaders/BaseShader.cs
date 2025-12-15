using System.Drawing;
using System.Numerics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace Caligo.Client.Graphics.Shaders;

public abstract class BaseShader
{
    private const int GlslVersion = 460;
    protected int? ProgramHandle;
    protected bool IsReloading;
    protected bool IsInitialized;
    public string Name { get; }
    private readonly Dictionary<string, (int Handle, ActiveUniformType Type)> _uniforms = [];
    private readonly Dictionary<string, object> _uniformValues = [];

    private readonly Dictionary<ShaderType, int> _shaderHandles = [];

#if DEBUG
    private readonly FileSystemWatcher _watcher;
#endif

    protected BaseShader(string directory, string name)
    {
        Name = name;

#if DEBUG
        Console.WriteLine($"[Shader {name}] Enabling watcher for {Path.GetFullPath(directory)}");
        // watch for shader changes
        _watcher = new FileSystemWatcher(Path.GetFullPath(directory), $"{Name}.*")
        {
            NotifyFilter = NotifyFilters.LastWrite
        };

        _watcher.Changed += (_, _) => Reload();
#endif
    }

    public void Initialize()
    {
        if (IsInitialized)
            return;

        if (ProgramHandle.HasValue)
        {
            GL.UseProgram(0);
            // check if the program is still exists
            if (GL.IsProgram(ProgramHandle.Value))
                GL.DeleteProgram(ProgramHandle.Value);
        }

        ProgramHandle = GL.CreateProgram();
        var label = Name + " Shader Program";
        GL.ObjectLabel(ObjectLabelIdentifier.Program, ProgramHandle.Value, label.Length, label);

        InitializeShaders();

        Link();

        foreach (var (_, shader) in _shaderHandles)
        {
            GL.DetachShader(ProgramHandle.Value, shader);
            GL.DeleteShader(shader);
        }

        UpdateUniforms();

        IsInitialized = true;
    }

    public void Reload()
    {
        if (IsReloading)
            return;

        IsInitialized = false;
        IsReloading = true;
        Console.WriteLine($"[Shader {Name}] Reloading shader");
        try
        {
            Dispose();
            Initialize();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Shader {Name}] Error reloading shader: {e.Message}");
        }

        IsReloading = false;
    }

    protected abstract void InitializeShaders();

    protected void LoadShader(ShaderType type, string path)
    {
        LoadShader(type, path, ProgramHandle);
    }

    protected void LoadShader(ShaderType type, string path, int? handle)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Shader file not found: {path}");

        if (!handle.HasValue) throw new Exception("Shader program not initialized, please call Initialize() first");

        var content = File.ReadAllText(path);

        if (!content.StartsWith("#version")) content = $"#version {GlslVersion}\n#line 2\n{content}";

        var shader = GL.CreateShader((OpenTK.Graphics.OpenGL.ShaderType)type);
        var name = Path.GetFileName(path).Replace('.', ' ');
        GL.ObjectLabel(ObjectLabelIdentifier.Shader, shader, name.Length, name);
        GL.ShaderSource(shader, content);

        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);

        if (code != (int)All.True)
        {
            GL.GetShaderInfoLog(shader, out var infoLog);
            var shaderType = Enum.GetName(typeof(ShaderType), type);

            // Print shader source and error message
            Console.WriteLine($"[Shader {name}] Error occurred whilst compiling {shaderType}: {infoLog}");
            Console.WriteLine($"Shader Source:\n{content.Replace("\n", "\n\t")}");

            throw new Exception($"Error occurred whilst compiling {shaderType} Shader({path}): {infoLog}");
        }

        GL.AttachShader(handle.Value, shader);

        _shaderHandles.Add(type, shader);
    }

    private void Link()
    {
        if (!ProgramHandle.HasValue)
            throw new Exception("Shader program not initialized, please call Initialize() first");

        GL.LinkProgram(ProgramHandle.Value);
        GL.GetProgram(ProgramHandle.Value, GetProgramParameterName.LinkStatus, out var code);

        if (code != (int)All.True)
            throw new Exception($"Error occurred whilst linking Shader: {GL.GetProgramInfoLog(ProgramHandle.Value)}");
    }

    private void UpdateUniforms()
    {
        if (!ProgramHandle.HasValue)
            throw new Exception("Shader program not initialized, please call Initialize() first");

        _uniforms.Clear();

        GL.GetProgram(ProgramHandle.Value, GetProgramParameterName.ActiveUniforms, out var count);

        for (var i = 0; i < count; i++)
        {
            var name = GL.GetActiveUniform(ProgramHandle.Value, i, out _, out var uniformType);
            var location = GL.GetUniformLocation(ProgramHandle.Value, name);
            _uniforms.Add(name, (location, uniformType));

            if (!_uniformValues.TryGetValue(name, out var value))
                continue;

            switch (uniformType)
            {
                case ActiveUniformType.Float:
                    SetUniform1(ActiveUniformType.Float, name, (float)value);
                    break;
                case ActiveUniformType.Int or ActiveUniformType.Sampler2D
                    or ActiveUniformType.Sampler2DArray:
                    SetUniform1(uniformType, name, (int)value);
                    break;
                case ActiveUniformType.FloatVec2:
                    var (x, y) = value is ValueTuple<float, float> tuple ? tuple : ((float, float))value;
                    SetVector2(name, x, y);
                    break;
                case ActiveUniformType.FloatVec3:
                    SetVector3(name, (OpenTK.Mathematics.Vector3)value);
                    break;
                case ActiveUniformType.FloatVec4:
                    SetVector4(name, (Vector4)value);
                    break;
                case ActiveUniformType.FloatMat4:
                    SetMatrix4(name, (Matrix4)value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public ShaderFrame Use()
    {
        if (!ProgramHandle.HasValue)
            throw new Exception("Shader program not initialized, please call Initialize() first");

        GL.UseProgram(ProgramHandle.Value);
        return new ShaderFrame(this);
    }

    private void SetUniform1(ActiveUniformType expectedType, string name, int value)
    {
        if (!_uniforms.TryGetValue(name, out var uniform))
            return;

        if (uniform.Type != expectedType)
            throw new Exception($"Uniform({name}) is not of type {uniform.Type}");

        _uniformValues[name] = value;

        if (IsReloading)
            return;

        GL.Uniform1(_uniforms[name].Handle, value);
    }

    private void SetUniform1(ActiveUniformType expectedType, string name, float value)
    {
        if (!_uniforms.TryGetValue(name, out var uniform))
            return;

        if (uniform.Type != expectedType)
            throw new Exception($"Uniform({name}) is not of type {uniform.Type}");

        _uniformValues[name] = value;

        if (IsReloading)
            return;

        GL.Uniform1(_uniforms[name].Handle, value);
    }

    public void SetInt(string name, int value)
    {
        SetUniform1(ActiveUniformType.Int, name, value);
    }

    public void SetFloat(string name, float value)
    {
        SetUniform1(ActiveUniformType.Float, name, value);
    }


    public void SetVector2(string name, Vector2 value)
    {
        SetVector2(name, value.X, value.Y);
    }

    public void SetVector2(string name, OpenTK.Mathematics.Vector2 value)
    {
        SetVector2(name, value.X, value.Y);
    }

    public void SetVector2(string name, float x, float y)
    {
        if (!_uniforms.TryGetValue(name, out var uniform))
            return;

        if (uniform.Type != ActiveUniformType.FloatVec2)
            throw new Exception($"Uniform({name}) is not of type {uniform.Type}");

        _uniformValues[name] = (x, y);

        if (IsReloading)
            return;

        GL.Uniform2(uniform.Handle, x, y);
    }

    public void SetVector3(string name, Vector3 value)
    {
        var openTkVector = new OpenTK.Mathematics.Vector3(value.X, value.Y, value.Z);
        SetVector3(name, openTkVector);
    }

    public void SetVector3(string name, OpenTK.Mathematics.Vector3 value)
    {
        if (!_uniforms.TryGetValue(name, out var uniform))
            return;

        if (uniform.Type != ActiveUniformType.FloatVec3)
            throw new Exception($"Uniform({name}) is not of type {uniform.Type}");

        _uniformValues[name] = value;

        if (IsReloading)
            return;

        GL.Uniform3(uniform.Handle, value.X, value.Y, value.Z);
    }

    public void SetColor(string name, Color color)
    {
        var vec4 = new Vector4(
            color.R / 255f,
            color.G / 255f,
            color.B / 255f,
            color.A / 255f
        );

        SetVector4(name, vec4);
    }

    public void SetVector4(string name, float x, float y, float z, float w)
    {
        var vec4 = new Vector4(x, y, z, w);
        SetVector4(name, vec4);
    }

    public void SetVector4(string name, Vector4 value)
    {
        if (!_uniforms.TryGetValue(name, out var uniform))
            return;

        if (uniform.Type != ActiveUniformType.FloatVec4)
            throw new Exception($"Uniform({name}) is not of type {uniform.Type}");

        _uniformValues[name] = value;

        if (IsReloading)
            return;

        GL.Uniform4(uniform.Handle, value);
    }

    public void SetMatrix4(string name, Matrix4x4 value)
    {
        var openTkMatrix = new Matrix4(
            value.M11, value.M12, value.M13, value.M14,
            value.M21, value.M22, value.M23, value.M24,
            value.M31, value.M32, value.M33, value.M34,
            value.M41, value.M42, value.M43, value.M44
        );

        SetMatrix4(name, openTkMatrix);
    }

    public void SetMatrix4(string name, Matrix4 value)
    {
        if (!_uniforms.TryGetValue(name, out var uniform))
            return;

        if (uniform.Type != ActiveUniformType.FloatMat4)
            throw new Exception($"Uniform({name}) is not of type FloatMat4");

        _uniformValues[name] = value;

        if (IsReloading)
            return;

        GL.UniformMatrix4(uniform.Handle, false, ref value);
    }

    public void SetTexture2D(string name, int textureUnit, int textureHandle)
    {
        GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
        GL.BindTexture(TextureTarget.Texture2D, textureHandle);
        SetUniform1(ActiveUniformType.Sampler2D, name, textureUnit);
    }

    public void SetTextureArray(string name, Texture2DArray textureArray, int unit = 0)
    {
        SetTextureArray(name, textureArray.Handle, unit);
    }

    public void SetTextureArray(string name, int handle, int unit = 0)
    {
        GL.ActiveTexture(TextureUnit.Texture0 + unit);
        GL.BindTexture(TextureTarget.Texture2DArray, handle);
        SetUniform1(ActiveUniformType.Sampler2DArray, name, unit);
    }

    public void Destroy()
    {
        if (ProgramHandle.HasValue)
        {
            GL.UseProgram(0);
            GL.DeleteProgram(ProgramHandle.Value);
            ProgramHandle = null;
        }

        _shaderHandles.Clear();
        _uniforms.Clear();
        _uniformValues.Clear();
        IsInitialized = false;
    }

    public void Dispose()
    {
        GL.UseProgram(0);
        if (ProgramHandle.HasValue)
        {
            GL.DeleteProgram(ProgramHandle.Value);
            _shaderHandles.Clear();
        }
    }
}

public readonly struct ShaderFrame : IDisposable
{
    private readonly BaseShader _shader;

    public ShaderFrame(BaseShader shader)
    {
        _shader = shader;
    }

    public void Dispose()
    {
        GL.UseProgram(0);
    }
}
namespace WorldGen.Renderer;

using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

public class ShaderBuffer<T> where T : struct
{
    private readonly BufferTarget _target;
    private readonly BufferUsageHint _usageHint;
    public int Size { get; protected set; }
    public int Handle { get; }

    public int Length => Size / Marshal.SizeOf(default(T));
    public int Stride => Marshal.SizeOf(default(T));
    private string? _name;

    public string Name
    {
        get => _name ?? "Unnamed";
        set => Label(value);
    }

    public static ShaderBuffer<T> Create(BufferTarget target, BufferUsageHint usageHint, int size) =>
    new(target, usageHint, size);

    public static ShaderBuffer<T> Create(BufferTarget target, BufferUsageHint usageHint, T[] data) =>
    new(target, usageHint, data);

    public ShaderBuffer(BufferTarget target, BufferUsageHint usageHint, int size)
    {
        GL.CreateBuffers(1, out int buffers);
        Handle = buffers;
        Size = size * Stride;
        _target = target;
        _usageHint = usageHint;
        Bind();
        GL.NamedBufferData(Handle, Size, IntPtr.Zero, _usageHint);
    }

    public ShaderBuffer(BufferTarget target, BufferUsageHint usageHint, T[] data) : this(target, usageHint, data.Length)
    {
        SetData(data);
    }

    private void Label(string name)
    {
        _name = name;
        GL.ObjectLabel(ObjectLabelIdentifier.Buffer, Handle, name.Length, name);
    }

    public void SetData(T[] data)
    {
        var oldSize = Size;
        Size = data.Length * Marshal.SizeOf(default(T));
        Bind();
        if (Size > oldSize)
        {
            Console.WriteLine($"Resizing {Name} buffer to " + Size);
            GL.NamedBufferData(Handle, Size, data, _usageHint);
        }
        else
            GL.NamedBufferSubData(Handle, IntPtr.Zero, Size, data);
    }

    public unsafe ReadOnlySpan<T> GetData()
    {
        Bind();
        var data = GL.MapBuffer(_target, BufferAccess.ReadOnly);
        ReadOnlySpan<T> span = new(data.ToPointer(), Size / Stride);
        GL.UnmapBuffer(_target);
        return span;
    }

    public void Update(T[] data)
    {
        Update(0, data);
    }

    public void Update(int offset, params T[] data)
    {
        Bind();
        GL.NamedBufferSubData(Handle, offset * Stride, data.Length * Stride, data);
    }

    public void Bind()
    {
        GL.BindBuffer(_target, Handle);
    }

    public void BindToBase(int index, BufferRangeTarget target = BufferRangeTarget.ShaderStorageBuffer)
    {
        Bind();
        GL.BindBufferBase(target, index, Handle);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(Handle);
    }
}
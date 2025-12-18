using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace Caligo.Client.Graphics;

public class ShaderBuffer
{
    internal static int _totalAllocatedBytes;
    public static int TotalAllocatedBytes => _totalAllocatedBytes;
}

public class ShaderBuffer<T> where T : struct
{
    private readonly BufferTarget _target;
    private readonly BufferUsageHint _usageHint;
    private string? _name;

    public ShaderBuffer(BufferTarget target, BufferUsageHint usageHint, int size)
    {
        GL.CreateBuffers(1, out int buffers);
        Handle = buffers;
        Size = size * Stride;
        _target = target;
        _usageHint = usageHint;
        Bind();
        GL.NamedBufferData(Handle, Size, IntPtr.Zero, _usageHint);

        Interlocked.Add(ref ShaderBuffer._totalAllocatedBytes, Size);
    }

    public ShaderBuffer(BufferTarget target, BufferUsageHint usageHint, Span<T> data) : this(target, usageHint,
        data.Length)
    {
        SetData(data);
    }

    public int Size { get; protected set; }
    public int Handle { get; }

    public int Length => Size / Marshal.SizeOf(default(T));
    public int Stride => Marshal.SizeOf(default(T));

    public string Name
    {
        get => _name ?? "Unnamed";
        set => Label(value);
    }

    public static ShaderBuffer<T> Create(BufferTarget target, BufferUsageHint usageHint, int size)
    {
        return new ShaderBuffer<T>(target, usageHint, size);
    }

    public static ShaderBuffer<T> Create(BufferTarget target, BufferUsageHint usageHint, Span<T> data)
    {
        return new ShaderBuffer<T>(target, usageHint, data);
    }

    private void Label(string name)
    {
        _name = name;
        GL.ObjectLabel(ObjectLabelIdentifier.Buffer, Handle, name.Length, name);
    }

    public unsafe void SetData(Span<T> data, bool orphan = false)
    {
        var oldSize = Size;
        Size = data.Length * Marshal.SizeOf(default(T));
        Bind();

        fixed (T* ptr = data)
        {
            var pointer = new IntPtr(ptr);
            if (Size > oldSize)
            {
                // Resize and upload new data
                GL.NamedBufferData(Handle, Size, pointer, _usageHint);
                Interlocked.Add(ref ShaderBuffer._totalAllocatedBytes, Size - oldSize);
            }
            else if (orphan)
            {
                // Orphan and upload new data
                GL.NamedBufferData(Handle, Size, IntPtr.Zero, _usageHint); // Orphan
                GL.NamedBufferSubData(Handle, IntPtr.Zero, Size, pointer); // Upload
            }
            else
            {
                // Direct upload (faster but may stall if GPU is reading)
                GL.NamedBufferSubData(Handle, IntPtr.Zero, Size, pointer);
            }
        }
    }

    public unsafe ReadOnlySpan<T> GetData()
    {
        Bind();
        var data = GL.MapBuffer(_target, BufferAccess.ReadOnly);
        ReadOnlySpan<T> span = new(data.ToPointer(), Size / Stride);
        GL.UnmapBuffer(_target);
        return span;
    }

    public void Resize(int newSize)
    {
        if (newSize <= 0)
            throw new ArgumentException("Size must be greater than zero.", nameof(newSize));

        if (newSize * Stride <= Size)
            return;

        var oldSize = Size;
        Size = newSize * Stride;
        Bind();
        GL.NamedBufferData(Handle, Size, IntPtr.Zero, _usageHint);
        Interlocked.Add(ref ShaderBuffer._totalAllocatedBytes, Size - oldSize);
    }

    public void Update(T[] data)
    {
        SetData(data);
    }

    public void Update(int offset, params T[] data)
    {
        Bind();
        Resize(data.Length + offset);
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
        Interlocked.Add(ref ShaderBuffer._totalAllocatedBytes, -Size);
        GL.DeleteBuffer(Handle);
    }
}
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace Caligo.Client.Graphics;

/// <summary>
///     Represents a single indirect draw command for OpenGL's MultiDrawArraysIndirect.
///     Each property maps to a field in the OpenGL DrawArraysIndirectCommand struct.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[Serializable]
public struct IndirectDrawCommand
{
    /// <summary>
    ///     The number of vertices to draw per instance (i.e., per face or quad).
    ///     For a quad rendered as a triangle strip, this is typically 4.
    /// </summary>
    public uint Count;

    /// <summary>
    ///     The number of instances to draw (e.g., number of faces or quads).
    /// </summary>
    public uint InstanceCount;

    /// <summary>
    ///     The starting index in the vertex buffer (usually 0 for non-indexed drawing).
    /// </summary>
    public uint First;

    /// <summary>
    ///     The base instance for instanced rendering (used as gl_InstanceID base).
    /// </summary>
    public uint BaseInstance;
}

internal static class IndirectDrawCommandExtensions
{
    /// <summary>
    ///     Checks if two IndirectDrawCommand instances are equal.
    /// </summary>
    /// <param name="a">The first command.</param>
    /// <param name="b">The second command.</param>
    /// <returns>True if all properties are equal, false otherwise.</returns>
    public static bool Equals(this IndirectDrawCommand a, IndirectDrawCommand b)
    {
        return a.Count == b.Count &&
               a.InstanceCount == b.InstanceCount &&
               a.First == b.First &&
               a.BaseInstance == b.BaseInstance;
    }
}

/// <summary>
///     A buffer for storing and issuing indirect draw commands for instanced rendering.
///     Use Append to add commands, and Draw to issue all commands in the buffer.
/// </summary>
public class IndirectBuffer : IDisposable
{
    private int _capacity;
    private IndirectDrawCommand[] _commands;
    private int _drawCount;
    private int _handle;

    public IndirectBuffer(int capacity = 500)
    {
        _capacity = capacity;
        _commands = new IndirectDrawCommand[capacity];
        _handle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, _handle);
        GL.BufferData(BufferTarget.DrawIndirectBuffer, Size, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        ShaderBuffer._totalAllocatedBytes += Size;
    }

    private int Size => Marshal.SizeOf<IndirectDrawCommand>() * _capacity;

    public void Dispose()
    {
        GL.DeleteBuffer(_handle);
        ShaderBuffer._totalAllocatedBytes -= Size;
    }

    public void Append(IndirectDrawCommand command)
    {
        if (_drawCount >= _capacity)
            throw new InvalidOperationException("IndirectBuffer overflow");

        _commands[_drawCount] = command;
        _drawCount++;
    }

    public void Draw(PrimitiveType primitive)
    {
        if (_drawCount == 0)
            return;

        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, _handle);

        // Upload only the commands we need
        var uploadSize = _drawCount * Marshal.SizeOf<IndirectDrawCommand>();
        GL.BufferSubData(BufferTarget.DrawIndirectBuffer, IntPtr.Zero, uploadSize, _commands);

        GL.MultiDrawArraysIndirect(primitive, IntPtr.Zero, _drawCount, 0);
    }

    public void Resize(int newCapacity)
    {
        ShaderBuffer._totalAllocatedBytes -= Size;
        _drawCount = 0;
        _capacity = newCapacity;

        ShaderBuffer._totalAllocatedBytes += Size;
        _commands = new IndirectDrawCommand[newCapacity];

        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, _handle);
        GL.DeleteBuffer(_handle);

        _handle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, _handle);
        GL.BufferData(BufferTarget.DrawIndirectBuffer, Size, IntPtr.Zero, BufferUsageHint.DynamicDraw);
    }

    public void Clear()
    {
        _drawCount = 0;
    }
}
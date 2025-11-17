using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace WorldGen.Graphics;

/// <summary>
/// Represents a single indirect draw command for OpenGL's MultiDrawArraysIndirect.
/// Each property maps to a field in the OpenGL DrawArraysIndirectCommand struct.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[Serializable]
public struct IndirectDrawCommand
{
    /// <summary>
    /// The number of vertices to draw per instance (i.e., per face or quad).
    /// For a quad rendered as a triangle strip, this is typically 4.
    /// </summary>
    public uint Count;
    /// <summary>
    /// The number of instances to draw (e.g., number of faces or quads).
    /// </summary>
    public uint InstanceCount;
    /// <summary>
    /// The starting index in the vertex buffer (usually 0 for non-indexed drawing).
    /// </summary>
    public uint First;
    /// <summary>
    /// The base instance for instanced rendering (used as gl_InstanceID base).
    /// </summary>
    public uint BaseInstance;
}

static class IndirectDrawCommandExtensions
{
    /// <summary>
    /// Checks if two IndirectDrawCommand instances are equal.
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
/// A buffer for storing and issuing indirect draw commands for instanced rendering.
/// Use Append to add commands, and Draw to issue all commands in the buffer.
/// </summary>
public class IndirectBuffer
{
    private readonly int _handle;
    private IntPtr _mappedPtr;
    private readonly int _capacity;
    private int Size => Marshal.SizeOf<IndirectDrawCommand>() * _capacity;
    private int _drawCount;

    public IndirectBuffer(int capacity = 500)
    {
        _capacity = capacity;
        _handle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, _handle);

        GL.BufferStorage(
            BufferTarget.DrawIndirectBuffer,
            Size,
            IntPtr.Zero,
            BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit | BufferStorageFlags.MapCoherentBit
        );

        _mappedPtr = GL.MapBufferRange(
            BufferTarget.DrawIndirectBuffer,
            IntPtr.Zero,
            Size,
            MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapPersistentBit | MapBufferAccessMask.MapCoherentBit
        );
    }

    public void Append(IndirectDrawCommand command)
    {
        if (_drawCount > _capacity)
            throw new InvalidOperationException("IndirectBuffer overflow");

        var dest = _mappedPtr + _drawCount * Marshal.SizeOf<IndirectDrawCommand>();
        Marshal.StructureToPtr(command, dest, false);
        _drawCount++;
    }

    public void Draw(PrimitiveType primitive)
    {
        GL.BindBuffer(BufferTarget.DrawIndirectBuffer, _handle);
        if (_drawCount > 0)
            GL.MultiDrawArraysIndirect(primitive, IntPtr.Zero, _drawCount, 0);
    }

    public void Clear()
    {
        Console.WriteLine($"Clearing IndirectBuffer with {_drawCount} commands");
        _drawCount = 0;
        if (_mappedPtr == IntPtr.Zero) return;
        GL.UnmapBuffer(BufferTarget.DrawIndirectBuffer);
        _mappedPtr = GL.MapBufferRange(
            BufferTarget.DrawIndirectBuffer,
            IntPtr.Zero,
            Size,
            MapBufferAccessMask.MapWriteBit | MapBufferAccessMask.MapPersistentBit | MapBufferAccessMask.MapCoherentBit
        );
    }

    public void Dispose()
    {
        if (_mappedPtr != IntPtr.Zero)
            GL.UnmapBuffer(BufferTarget.DrawIndirectBuffer);
        GL.DeleteBuffer(_handle);
    }
}

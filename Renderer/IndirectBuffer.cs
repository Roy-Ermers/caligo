using OpenTK.Graphics.OpenGL4;

namespace WorldGen.Renderer;

public struct IndirectDrawCommand
{
    public uint Count;
    public uint InstanceCount;
    public uint FirstIndex;
    public int BaseVertex;
    public uint BaseInstance;
}

public class IndirectBuffer : ShaderBuffer<IndirectDrawCommand>
{
    public IndirectBuffer() : base(BufferTarget.DrawIndirectBuffer, BufferUsageHint.StaticRead, 0)
    {
        Name = "IndirectDrawBuffer";
    }
    

    public void Append(IndirectDrawCommand command)
    {
        var oldSize = Size;
        Size += Stride;
        Bind();
        GL.NamedBufferSubData(Handle, oldSize, Size - oldSize, ref command);
    }

}

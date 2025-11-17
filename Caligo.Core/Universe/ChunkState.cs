namespace Caligo.Core.Universe;

[Flags]
public enum ChunkState
{
    None = 1,
    Created = 2,
    Generating = 4,
    Generated = 8,
    Meshing = 16,
    Meshed = 32
}

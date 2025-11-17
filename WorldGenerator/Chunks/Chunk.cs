using WorldGen.Resources.Block;
using WorldGen.Utils;
using WorldGen.WorldGenerator.Chunks;

namespace WorldGen.WorldGenerator;

public struct Chunk
{
    public const int Size = 16;

    public ChunkPosition Position { get; }

    public readonly int Id => Position.Id;

    private readonly ushort[] data = new ushort[Chunk.Size * Chunk.Size * Chunk.Size];

    private ushort blockCount = 0;

    public readonly int BlockCount => blockCount;

    public Chunk(ChunkPosition position)
    {
        Position = position;
        data = new ushort[Chunk.Size * Chunk.Size * Chunk.Size];
        blockCount = 0;
    }

    public ushort this[int x, int y, int z]
    {
        readonly get => Get(x, y, z);
        set => Set(x, y, z, value);
    }

    public void Set(ChunkPosition position, Block block) => Set(position.X, position.Y, position.Z, block);
    public void Set(ChunkPosition position, ushort value) => Set(position.X, position.Y, position.Z, value);
    public void Set(int x, int y, int z, Block block) => Set(x, y, z, block.NumericId);
    public void Set(int x, int y, int z, ushort value)
    {
        var index = ZOrderCurve.GetIndex(x, y, z, Size);
        var currentBlock = data[index];

        if (value > 0 && currentBlock == 0)
            blockCount++;
        else if (value == 0 && currentBlock > 0)
            blockCount--;

        data[index] = value;
    }

    public readonly ushort Get(ChunkPosition position) => Get(position.X, position.Y, position.Z);

    public readonly ushort Get(int x, int y, int z)
    {
        var index = ZOrderCurve.GetIndex(x, y, z, Size);
        return data[index];
    }

    public readonly bool TryGet(ChunkPosition position, out ushort value) => TryGet(position.X, position.Y, position.Z, out value);

    public readonly bool TryGet(int x, int y, int z, out ushort value)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size)
        {
            value = 0;
            return false;
        }

        var index = ZOrderCurve.GetIndex(x, y, z, Size);
        if (index < 0 || index >= data.Length)
        {
            value = 0;
            return false;
        }

        value = data[index];
        return value > 0;
    }

    public override readonly int GetHashCode()
    {
        return Id;
    }
}

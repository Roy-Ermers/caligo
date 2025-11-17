using WorldGen.Resources.Block;
using WorldGen.Universe.PositionTypes;
using WorldGen.Utils;

namespace WorldGen.Universe;

public class Chunk
{
    public const int Size = 16;

    public ChunkPosition Position { get; }

    public ChunkState State { get; set; } = ChunkState.Created;

    public int Id => Position.Id;

    private readonly ushort[] data = new ushort[Size * Size * Size];

    private ushort blockCount = 0;

    public int BlockCount => blockCount;

    public Chunk(ChunkPosition position)
    {
        Position = position;
        data = new ushort[Size * Size * Size];
        blockCount = 0;
    }

    public ushort this[int x, int y, int z]
    {
        get => Get(x, y, z);
        set => Set(x, y, z, value);
    }

    public void Set(ChunkLocalPosition position, Block block) => Set(position.X, position.Y, position.Z, block);
    public void Set(ChunkLocalPosition position, ushort value) => Set(position.X, position.Y, position.Z, value);
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

    public ushort Get(ChunkLocalPosition position) => Get(position.X, position.Y, position.Z);

    public ushort Get(int x, int y, int z)
    {
        var index = ZOrderCurve.GetIndex(x, y, z, Size);
        return data[index];
    }

    public bool TryGet(ChunkLocalPosition position, out ushort value) => TryGet(position.X, position.Y, position.Z, out value);

    public bool TryGet(int x, int y, int z, out ushort value)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size)
        {
            value = 0;
            return false;
        }


        value = Get(x, y, z);
        return value > 0;
    }

    public override int GetHashCode()
    {
        return Id;
    }
}

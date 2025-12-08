using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Utils;

namespace Caligo.Core.Universe;

public class Chunk
{
    public const int Size = 16;

    private readonly ushort[] data = new ushort[Size * Size * Size];

    private ushort blockCount;

    public Chunk(ChunkPosition position)
    {
        Position = position;
        data = new ushort[Size * Size * Size];
        blockCount = 0;
    }

    public ChunkPosition Position { get; }

    public ChunkState State { get; set; } = ChunkState.Created;

    public int Id => Position.Id;

    public int BlockCount => blockCount;

    public BoundingBox BoundingBox => new(
        Position.ToWorldPosition(),
        Size,
        Size,
        Size
    );

    public ushort this[int x, int y, int z]
    {
        get => Get(x, y, z);
        set => Set(x, y, z, value);
    }

    public void Set(ChunkLocalPosition position, Block block)
    {
        Set(position.X, position.Y, position.Z, block);
    }

    public void Set(ChunkLocalPosition position, ushort value)
    {
        Set(position.X, position.Y, position.Z, value);
    }

    public void Set(int x, int y, int z, Block block)
    {
        Set(x, y, z, block.NumericId);
    }

    public void Set(int x, int y, int z, ushort value)
    {
        var index = MortonCurve.Encode(x, y, z);
        var currentBlock = data[index];

        if (value > 0 && currentBlock == 0)
            blockCount++;
        else if (value == 0 && currentBlock > 0)
            blockCount--;

        data[index] = value;
    }

    public ushort Get(ChunkLocalPosition position)
    {
        return Get(position.X, position.Y, position.Z);
    }

    public ushort Get(int x, int y, int z)
    {
        var index = MortonCurve.Encode(x, y, z);
        return data[index];
    }

    public bool TryGet(ChunkLocalPosition position, out ushort value)
    {
        return TryGet(position.X, position.Y, position.Z, out value);
    }

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
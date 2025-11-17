using System.Numerics;
using WorldGen.Utils;

namespace WorldGen.Universe.PositionTypes;

public readonly record struct ChunkLocalPosition(int X, int Y, int Z)
{
    public static readonly ChunkLocalPosition Zero = new(0, 0, 0);

    public readonly int Id => HashCode.Combine(X, Y, Z);

    public int Index => ZOrderCurve.GetIndex(X, Y, Z);

    public readonly WorldPosition ToWorldPosition(ChunkPosition chunkPosition)
    {
        var (chunkX, chunkY, chunkZ) = chunkPosition.ToWorldPosition();
        return new WorldPosition(chunkX + X, chunkY + Y, chunkZ + Z);
    }

    public readonly override int GetHashCode()
    {
        return Id;
    }

    public readonly override string ToString()
    {
        return $"(L;{X}, {Y}, {Z})";
    }

    public static ChunkLocalPosition FromWorldPosition(WorldPosition position) =>
        FromWorldPosition(position.X, position.Y, position.Z);

    public static ChunkLocalPosition FromWorldPosition(int x, int y, int z)
    {
        return new ChunkLocalPosition
        {
            X = MathExtensions.Mod(x, Chunk.Size),
            Y = MathExtensions.Mod(y, Chunk.Size),
            Z = MathExtensions.Mod(z, Chunk.Size),
        };
    }

    public static ChunkLocalPosition FromIndex(int index) => ZOrderCurve.GetPosition(index, Chunk.Size);

    public static implicit operator ChunkLocalPosition((int X, int Y, int Z) position) =>
        new(position.X, position.Y, position.Z);

    public static implicit operator Vector3(ChunkLocalPosition position) => new(position.X, position.Y, position.Z);

    public static ChunkLocalPosition operator +(ChunkLocalPosition left, ChunkLocalPosition right)
    {
        return new ChunkLocalPosition(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    public static ChunkLocalPosition operator -(ChunkLocalPosition left, ChunkLocalPosition right)
    {
        return new ChunkLocalPosition(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    public static ChunkLocalPosition operator +(ChunkLocalPosition left, Vector3 right)
    {
        return new ChunkLocalPosition(left.X + (int)right.X, left.Y + (int)right.Y, left.Z + (int)right.Z);
    }

    public static ChunkLocalPosition operator -(ChunkLocalPosition left, Vector3 right)
    {
        return new ChunkLocalPosition(left.X - (int)right.X, left.Y - (int)right.Y, left.Z - (int)right.Z);
    }
}
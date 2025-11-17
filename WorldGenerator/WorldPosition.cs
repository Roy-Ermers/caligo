
using WorldGen.WorldGenerator.Chunks;

namespace WorldGen.WorldGenerator;

public readonly record struct WorldPosition(int X, int Y, int Z)
{
    public static readonly WorldPosition Zero = new(0, 0, 0);

    public int X { get; init; } = X;
    public int Y { get; init; } = Y;
    public int Z { get; init; } = Z;

    public readonly int Id => HashCode.Combine(X, Y, Z);

    public ChunkPosition ChunkPosition => ChunkPosition.FromWorldPosition(this);

    override public readonly int GetHashCode()
    {
        return Id;
    }

    public override readonly string ToString()
    {
        return $"(W;{X}, {Y}, {Z})";
    }
}

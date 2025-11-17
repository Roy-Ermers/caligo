using System.Collections;
using WorldGen.Universe;
using WorldGen.Universe.PositionTypes;

namespace WorldGen.Utils;

public readonly struct CubeIterator(WorldPosition start, WorldPosition end) : IEnumerable<WorldPosition>
{
    public readonly WorldPosition Start = start;
    public readonly WorldPosition End = end;

    public CubeIterator(Chunk chunk) : this(
        chunk.Position.ToWorldPosition(),
        chunk.Position.ToWorldPosition() + (Chunk.Size - 1)
    )
    {
    }

    public IEnumerator<WorldPosition> GetEnumerator()
    {
        for (int x = Start.X; x <= End.X; x++)
        {
            for (int y = Start.Y; y <= End.Y; y++)
            {
                for (int z = Start.Z; z <= End.Z; z++)
                {
                    yield return new WorldPosition(x, y, z);
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

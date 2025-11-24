using System.Collections;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe;

namespace Caligo.Core.Spatial;

public readonly struct CubeIterator : IEnumerable<WorldPosition>
{
    public readonly WorldPosition Start;
    public readonly WorldPosition End;

    public CubeIterator(Chunk chunk) : this(
        chunk.Position.ToWorldPosition(),
        chunk.Position.ToWorldPosition() + (Chunk.Size)
    )
    {
    }

    public CubeIterator(WorldPosition start, WorldPosition end)
    {
        Start = start;
        End = end;
    }

    public IEnumerator<WorldPosition> GetEnumerator()
    {
        for (var x = Start.X; x < End.X; x++)
        {
            for (var y = Start.Y; y < End.Y; y++)
            {
                for (var z = Start.Z; z < End.Z; z++)
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

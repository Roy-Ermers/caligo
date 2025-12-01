using System.Collections;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe;
using Caligo.Core.Utils;

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
        var size = End - Start;
        var count = size.X * size.Y * size.Z;

        for (var i = 0; i < count; i++)
        {
            var pos = ZOrderCurve.GetPosition(i, size.X, size.Y, size.Z);
            yield return Start + new WorldPosition(pos.x % size.X, pos.y % size.Y, pos.z % size.Z);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
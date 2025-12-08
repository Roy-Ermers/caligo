using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Utils;

namespace Caligo.Client.Renderer.Worlds.Mesh;

public record struct ChunkMesh
{
    public static readonly ChunkMesh Empty = new();
    private readonly SortedList<Direction, int> _directionOffsets = [];


    private readonly int[] _renderData = [];

    public ChunkMesh(IDictionary<Direction, List<BlockFaceRenderData>> data, ChunkPosition position)
    {
        List<int> renderData = [];

        foreach (var pair in data)
        {
            _directionOffsets.Add(pair.Key, renderData.Count);
            foreach (var face in pair.Value) renderData.AddRange(face.Encode());
        }

        _renderData = [..renderData];
        Position = position;
    }

    public IReadOnlyList<int> RenderData => _renderData;
    public ChunkPosition Position { get; init; }

    public Span<int> GetFacesForDirection(Direction direction)
    {
        if (!_directionOffsets.TryGetValue(direction, out var startIndex))
            return [];

        var endIndex = _renderData.Length;

        var directionIndex = _directionOffsets.IndexOfKey(direction);
        if (directionIndex < _directionOffsets.Count - 1)
            endIndex = _directionOffsets.GetValueAtIndex(directionIndex + 1);

        return _renderData.AsSpan().Slice(startIndex, endIndex - startIndex);
    }
}
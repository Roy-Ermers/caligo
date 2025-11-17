using System.Collections.Frozen;
using WorldGen.Resources.Block;
using WorldGen.Universe.PositionTypes;

namespace WorldGen.Renderer.Worlds.Mesh;

public readonly record struct ChunkMesh
{
	public static readonly ChunkMesh Empty = new()
	{
		Faces = FrozenDictionary<Direction, List<BlockFaceRenderData>>.Empty,
		Position = ChunkPosition.Zero
	};
	internal static readonly int MaxFacesPerChunk = 16 * 16 * 16 * 6 / 2;

	public FrozenDictionary<Direction, List<BlockFaceRenderData>> Faces { get; init; }
	public ChunkPosition Position { get; init; }
	public readonly int TotalFaceCount;

	public ChunkMesh(FrozenDictionary<Direction, List<BlockFaceRenderData>> faces, ChunkPosition position)
	{
		Faces = faces;
		Position = position;

		var count = 0;
		foreach (var (_, list) in Faces)
			count += list.Count;
		TotalFaceCount = count;
	}

	public readonly int[] GetEncodedFaces()
	{
		// Calculate total number of faces
		var totalFaces = 0;
		foreach (var (_, faces) in Faces)
			totalFaces += faces.Count;

		// Pre-allocate array (each face encodes to 2 ints)
		var encoded = new int[totalFaces * 2];
		var index = 0;

		// Encode all faces
		foreach (var (_, faces) in Faces)
		{
			foreach (var face in faces)
			{
				var faceData = face.Encode();
				encoded[index++] = faceData[0];
				encoded[index++] = faceData[1];
			}
		}

		return encoded;
	}
}

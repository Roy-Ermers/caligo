using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using OpenTK.Graphics.ES11;
using WorldGen.ChunkRenderer.Materials;
using WorldGen.ChunkRenderer.Mesh;
using WorldGen.ModuleSystem;
using WorldGen.ModuleSystem.Storage;
using WorldGen.Resources.Atlas;
using WorldGen.Resources.Block;
using WorldGen.Utils;
using WorldGen.WorldGenerator;
using WorldGen.WorldGenerator.Chunks;

namespace WorldGen.ChunkRenderer;

public class ChunkMesher(ResourceTypeStorage<Block> blockStorage, Atlas blockTextureAtlas, MaterialBuffer materialBuffer)
{
    public const string AtlasIdentifier = $"{Identifier.MainModule}:block_atlas";

    private readonly MaterialBuffer _materialBuffer = materialBuffer;
    private readonly ResourceTypeStorage<Block> _blockStorage = blockStorage;
    private readonly Atlas BlockTextureAtlas = blockTextureAtlas;

    private readonly BlockingCollection<Chunk> _chunkQueue = [];
    public readonly ConcurrentQueue<ChunkMesh> Meshes = [];

    public void EnqueueChunk(Chunk chunk)
    {
        if (chunk.BlockCount == 0)
            return; // No need to mesh empty chunks

        _chunkQueue.Add(chunk);
    }

    public void StartProcessing()
    {
        Task.Run(() =>
        {
            while (!_chunkQueue.IsCompleted)
            {
                try
                {
                    var chunk = _chunkQueue.Take();

                    var mesh = GenerateMesh(chunk);

                    Meshes.Enqueue(mesh);
                }
                catch (InvalidOperationException)
                {
                    // The collection was marked as complete, exit the loop
                    break;
                }
            }
        });
    }

    public void GetChunkMeshes(List<ChunkMesh> meshes)
    {
        while (Meshes.TryDequeue(out var mesh))
            meshes.Add(mesh);
    }

    private ChunkMesh GenerateMesh(Chunk chunk)
    {
        using var profiler = new Profiler() { Name = "ChunkMesher.GenerateMesh" };

        if (chunk.BlockCount == 0)
        {
            return new ChunkMesh
            {
                Faces = ChunkMesh.Empty.Faces,
                Position = chunk.Position
            };
        }

        var faces = new Dictionary<Direction, List<BlockFaceRenderData>>();

        for (short i = 0; i < Math.Pow(Chunk.Size, 3); i++)
        {
            Console.WriteLine($"Processing block {i + 1}/{Math.Pow(Chunk.Size, 3)} in chunk {chunk.Position}");
            var position = ChunkPosition.FromIndex(i);

            // tryGet skips air blocks, so we only process non-air blocks
            if (!chunk.TryGet(position, out var blockId))
                continue;

            var block = _blockStorage[blockId];
            if (block is null)
            {
                Console.WriteLine($"Block with ID {blockId} not found in storage.");
                continue;
            }

            var model = block.Model;

            // nothing to render.
            if (model is null)
                continue;

            for (var direction = (Direction)0; direction <= (Direction)5; direction++)
            {
                if (chunk.TryGet(position + direction.ToVector3(), out var neighborBlockId))
                {
                    var neighborBlock = _blockStorage[neighborBlockId];
                    var neighborModel = neighborBlock?.Model;

                    if (neighborModel is not null && (neighborModel.Culling?.IsCullingEnabled(direction.Opposite()) ?? false))
                    {
                        // Don't render face if culling is enabled and neighbor block is not air
                        continue;
                    }
                }

                foreach (var element in model.Elements)
                {
                    var newFace = element.ToRenderData(direction, position, block.Textures ?? [], _materialBuffer, BlockTextureAtlas);
                    if (newFace is null)
                        continue;

                    if (faces.TryGetValue(direction, out var faceList))
                        faceList.Add(newFace.Value);
                    else
                    {
                        faceList = [newFace.Value];
                        faces.Add(direction, faceList);
                    }
                }
            }
        }

        return new ChunkMesh
        {
            Faces = faces.ToFrozenDictionary(),
            Position = chunk.Position
        };
    }
}

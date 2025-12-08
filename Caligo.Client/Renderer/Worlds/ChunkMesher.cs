using System.Collections.Concurrent;
using Caligo.Client.Renderer.Worlds.Materials;
using Caligo.Client.Renderer.Worlds.Mesh;
using Caligo.Client.Resources.Atlas;
using Caligo.Core.FileSystem.Images;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe;
using Caligo.Core.Utils;
using Caligo.ModuleSystem;
using Caligo.ModuleSystem.Storage;
using Random = Caligo.Core.Utils.Random;

namespace Caligo.Client.Renderer.Worlds;

public class ChunkMesher
{
    public const string AtlasIdentifier = $"{Identifier.MainModule}:block_atlas";
    private readonly ResourceTypeStorage<Block> _blockStorage;

    private readonly BlockingCollection<Chunk> _chunkQueue = [];
    private readonly MaterialBuffer _materialBuffer;
    private readonly ConcurrentQueue<ChunkMesh> Meshes = [];

    public ChunkMesher(ResourceTypeStorage<Block> blockStorage, ModuleRepository repository,
        MaterialBuffer materialBuffer)
    {
        _blockStorage = blockStorage;
        _materialBuffer = materialBuffer;

        BlockTextureAtlas = BuildAtlas(repository);
    }

    public Atlas BlockTextureAtlas { get; }

    internal Atlas BuildAtlas(ModuleRepository repository)
    {
        var storage = repository.GetAll<Image>();
        var atlas = new AtlasBuilder();

        foreach (var image in storage)
            atlas.AddEntry(image.Key, image.Value);

        return atlas.Build();
    }

    public void EnqueueChunk(Chunk chunk)
    {
        if (chunk.BlockCount == 0)
        {
            chunk.State |= ChunkState.Meshed;
            return; // No need to mesh empty chunks
        }

        chunk.State |= ChunkState.Meshing;
        _chunkQueue.Add(chunk);
    }

    public bool TryDequeue(out ChunkMesh mesh)
    {
        if (Meshes.TryDequeue(out mesh))
            return true;

        mesh = default;
        return false;
    }

    public void StartProcessing()
    {
        for (var processor = 0; processor < 1; processor++)
        {
            var thread = new Thread(Process)
            {
                IsBackground = true,
                Name = $"ChunkMesherThread {processor}"
            };
            thread.Start();
        }
    }

    private void Process()
    {
        while (!_chunkQueue.IsCompleted)
        {
            var chunk = _chunkQueue.Take();
            var mesh = GenerateMesh(chunk);
            Meshes.Enqueue(mesh);
        }
    }

    private (short x, short y, short z) GetBlockOffset(WorldPosition worldPosition, string? offsetType, Random random)
    {
        if (string.IsNullOrEmpty(offsetType)) return (0, 0, 0);

        var offsetX = (short)(offsetType.Contains('x') ? random.Next(-7, 7) : 0);
        var offsetY = (short)(offsetType.Contains('y') ? random.Next(-7, 7) : 0);
        var offsetZ = (short)(offsetType.Contains('z') ? random.Next(-7, 7) : 0);
        return (offsetX, offsetY, offsetZ);
    }

    private ChunkMesh GenerateMesh(Chunk chunk)
    {
        Random random = new(chunk.Id);

        var world = Game.Instance.world;
        if (chunk.BlockCount == 0)
            return ChunkMesh.Empty with
            {
                Position = chunk.Position
            };

        var faces = new Dictionary<Direction, List<BlockFaceRenderData>>();

        for (short i = 0; i < Math.Pow(Chunk.Size, 3); i++)
        {
            var position = ChunkLocalPosition.FromIndex(i);
            var worldPosition = position.ToWorldPosition(chunk.Position);
            // tryGet skips air blocks, so we only process non-air blocks
            if (!world.TryGetBlock(worldPosition, out var blockId))
                continue;

            if (!_blockStorage.TryGetValue(blockId, out var block))
            {
                Console.WriteLine($"Block with ID {blockId} not found in storage.");
                continue;
            }

            var variant = block.GetVariant(worldPosition.Id);
            // nothing to render.
            if (variant is null)
                continue;

            var offset = GetBlockOffset(worldPosition, variant.Value.Model.OffsetType, random);

            for (var direction = (Direction)0; direction <= (Direction)5; direction++)
            {
                if (world.TryGetBlock(worldPosition + direction.ToVector3(), out var neighborBlockId))
                {
                    var neighborBlock = _blockStorage[neighborBlockId];
                    var neighborModel = neighborBlock?.GetRandomVariant(random);

                    if (neighborModel is not null &&
                        (neighborModel.Value.Model!.Culling?.IsCullingEnabled(direction.Opposite()) ?? false))
                        // Don't render face if culling is enabled and neighbor block is not air
                        continue;
                }

                foreach (var element in variant.Value.Model.Elements.Reverse())
                {
                    var newFace = element.ToRenderData(direction, position, variant.Value.Textures ?? [],
                        _materialBuffer, offset, BlockTextureAtlas);
                    if (newFace is null)
                        continue;

                    if (faces.TryGetValue(direction, out var faceList))
                    {
                        faceList.Add(newFace.Value);
                    }
                    else
                    {
                        faceList = [newFace.Value];
                        faces.Add(direction, faceList);
                    }
                }
            }
        }

        chunk.State |= ChunkState.Meshed;
        chunk.State &= ~ChunkState.Meshing;

        return new ChunkMesh(
            faces,
            chunk.Position
        );
    }
}
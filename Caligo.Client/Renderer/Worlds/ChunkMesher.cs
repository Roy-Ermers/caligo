using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Caligo.Client.Renderer.Worlds.Materials;
using Caligo.Client.Renderer.Worlds.Mesh;
using Caligo.Client.Resources.Atlas;
using Caligo.Core.FileSystem.Images;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe;
using Caligo.Core.Universe.Worlds;
using Caligo.Core.Utils;
using Caligo.ModuleSystem;
using Caligo.ModuleSystem.Storage;
using Identifier = Caligo.ModuleSystem.Identifier;
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
        var watch = Stopwatch.StartNew();

        Random random = new(chunk.Id);

        var world = Game.Instance.World;
        if (chunk.BlockCount == 0)
            return ChunkMesh.Empty with
            {
                Position = chunk.Position
            };

        var faces = new Dictionary<Direction, List<BlockFaceRenderData>>();

        for (short i = 0; i < Math.Pow(Chunk.Size, 3); i++)
        {
            var blockFaces = ProcessBlock(i, chunk, world, random);
            foreach (var face in blockFaces)
            {
                if (!faces.ContainsKey(face.Normal))
                    faces[face.Normal] = [];

                faces[face.Normal].Add(face);
            }
        }

        chunk.State |= ChunkState.Meshed;
        chunk.State &= ~ChunkState.Meshing;

        Statistics.ChunkMesherSpeedGauge.Record(watch.Elapsed.Microseconds / 1000f);

        return new ChunkMesh(
            faces,
            chunk.Position
        )
        {
            BoundingBox = chunk.BoundingBox
        };
    }

    private IEnumerable<BlockFaceRenderData> ProcessBlock(short index, Chunk chunk, World world, Random random)
    {
        var position = ChunkLocalPosition.FromIndex(index);
        var worldPosition = position.ToWorldPosition(chunk.Position);
        // tryGet skips air blocks, so we only process non-air blocks
        if (!world.TryGetBlock(worldPosition, out var blockId))
            yield break;

        if (!_blockStorage.TryGetValue(blockId, out var block))
        {
            Console.WriteLine($"Block with ID {blockId} not found in storage.");
            yield break;
        }

        var variant = block.GetVariant(worldPosition.Id);
        // nothing to render.
        if (variant is null)
            yield break;


        var model = variant.Value.Model;
        for (var direction = (Direction)0; direction <= (Direction)5; direction++)
        {
            if (ShouldCullFace(worldPosition, world, direction, random))
                continue;

            foreach (var element in model.Elements.Reverse())
            {
                var face = element.TextureFaces[direction];
                if (face?.Texture == null)
                    continue;

                var textureKey = face.Value.TextureVariable;
                var texture = variant.Value.PickTexture(textureKey, random);
                var textureId = BlockTextureAtlas[texture];

                var material = new Material
                {
                    Width = (ushort)element.Size.X,
                    Height = (ushort)element.Size.Y,
                    TextureId = textureId,
                    UV0 = new Vector2(face.Value.UV.X, face.Value.UV.Y),
                    UV1 = new Vector2(face.Value.UV.Z, face.Value.UV.W),
                    Tint = face.Value.Tint,
                    Shade = face.Value.Shade
                };

                var facePosition = element.CalculateFacePosition(
                    direction,
                    position,
                    model.OffsetType,
                    random
                );

                var materialId = _materialBuffer.Add(material);

                yield return new BlockFaceRenderData
                {
                    Normal = direction,
                    MaterialId = materialId,
                    X = facePosition.x,
                    Y = facePosition.y,
                    Z = facePosition.z,
                    Light = Vector4.One * 15
                };
            }
        }
    }

    private bool ShouldCullFace(WorldPosition worldPosition, World world, Direction direction, Random random)
    {
        var neighborPosition = worldPosition + direction.ToVector3();
        if (!world.TryGetBlock(neighborPosition, out var neighborBlockId))
        {
            return false;
        }

        var block = _blockStorage[neighborBlockId];
        var variant = block?.GetVariant(neighborPosition.Id);
        var model = variant?.Model;

        return model is not null && (model.Culling?.IsCullingEnabled(direction.Opposite()) ?? false);
    }
}
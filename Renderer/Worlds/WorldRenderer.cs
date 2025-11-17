using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using WorldGen.Renderer.Worlds.Materials;
using WorldGen.Renderer.Worlds.Mesh;
using WorldGen.ModuleSystem.Storage;
using WorldGen.Graphics;
using WorldGen.Graphics.Shaders;
using WorldGen.Resources.Atlas;
using WorldGen.Resources.Block;
using WorldGen.Utils;

using Vector4 = OpenTK.Mathematics.Vector4;
using System.Collections.Concurrent;
using WorldGen.Universe.PositionTypes;
using WorldGen.Universe;

namespace WorldGen.Renderer.Worlds;


public class WorldRenderer
{
    private readonly World World;
    private readonly MaterialBuffer MaterialBuffer;
    public readonly ChunkMesher ChunkMesher;

    private readonly Atlas _blockTextureAtlas;

    public readonly ShaderBuffer<int> _materialShaderBuffer;
    public readonly ShaderBuffer<int> _faceShaderBuffer;
    public readonly ShaderBuffer<Vector4> _chunkInfoShaderBuffer;
    public readonly IndirectBuffer _indirectBuffer;

    private int _quadVertexArrayObject;

    private readonly Dictionary<ChunkPosition, ChunkMesh> _meshes = [];
    public ConcurrentQueue<ChunkPosition> UnloadQueue = [];

    private readonly TrackedQueue<Vector4> _chunkPositions = new();
    private readonly TrackedQueue<int> _faces = new();


    public WorldRenderer(
        World world,
        Atlas blockTextureAtlas,
        ResourceTypeStorage<Block> blockStorage
    )
    {
        World = world;
        MaterialBuffer = new MaterialBuffer();
        _blockTextureAtlas = blockTextureAtlas;
        ChunkMesher = new ChunkMesher(blockStorage, blockTextureAtlas, MaterialBuffer);

        _materialShaderBuffer = ShaderBuffer<int>.Create(
            BufferTarget.ShaderStorageBuffer,
            BufferUsageHint.DynamicDraw,
            Math.Max(1, MaterialBuffer.EncodedLength)
        );
        _materialShaderBuffer.Name = "MaterialBuffer";

        _chunkInfoShaderBuffer = ShaderBuffer<Vector4>.Create(
            BufferTarget.ShaderStorageBuffer,
            BufferUsageHint.DynamicDraw,
            1
        );
        _chunkInfoShaderBuffer.Name = "ChunkInfoBuffer";

        _faceShaderBuffer = ShaderBuffer<int>.Create(
            BufferTarget.ShaderStorageBuffer,
            BufferUsageHint.DynamicDraw,
            100_000 // Initial size, will grow as needed
        );
        _faceShaderBuffer.Name = "blockFaceBuffer";

        _indirectBuffer = new IndirectBuffer(100000);

        UpdateMaterialBuffer(true);

        CreateQuadVertexArrayObject();

        ChunkMesher.StartProcessing();
    }

    public void Clear()
    {
        _chunkInfoShaderBuffer.SetData([]);
        _faceShaderBuffer.SetData([]);
        _indirectBuffer.Clear();
        MaterialBuffer.Clear();
        _materialShaderBuffer.SetData([]);
        _chunkPositions.Reset();
        _faces.Reset();
        _meshes.Clear();
    }

    private void UpdateMaterialBuffer(bool force = false)
    {
        if (!force && !MaterialBuffer.IsDirty)
            return;

        _materialShaderBuffer.SetData(MaterialBuffer.Encode());
        _materialShaderBuffer.Bind();
    }

    private void UpdateFaceBuffer()
    {
        if (_meshes.Count == 0)
            return;

        _chunkPositions.Reset();
        _indirectBuffer.Clear();

        var faceIndex = 0;
        foreach (var loader in World.ChunkLoaders)
        {
            if (!_meshes.ContainsKey(loader.Position))
                continue;

            var mesh = _meshes[loader.Position];

            var startingFaceIndex = faceIndex;
            foreach (var (direction, faces) in mesh.Faces)
            {
                faceIndex += faces.Count;
                _faces.EnqueueRange(faces.SelectMany(f => f.Encode()));
            }

            var (x, y, z) = mesh.Position.ToWorldPosition();
            _chunkPositions.Enqueue(new Vector4(x, y, z, 0));

            _indirectBuffer.Append(new()
            {
                Count = 4,
                InstanceCount = (uint)(faceIndex - startingFaceIndex),
                First = 0,
                BaseInstance = (uint)startingFaceIndex
            });
        }

        if (_chunkPositions.TryUpdate(out var updatedChunkPositions))
            _chunkInfoShaderBuffer.SetData([.. updatedChunkPositions]);

        if (_faces.TryUpdate(out var updatedFaces))
        {
            _faceShaderBuffer.SetData([.. updatedFaces]);
            UpdateMaterialBuffer();
        }
    }

    public void Update()
    {
        var changed = false;
        while (ChunkMesher.TryDequeue(out var mesh))
        {
            _meshes[mesh.Position] = mesh;
            changed = true;
        }

        if (changed)
            UpdateFaceBuffer();

        foreach (var chunkLoader in World.ChunkLoaders)
        {
            if (_meshes.ContainsKey(chunkLoader.Position))
                continue;

            if (!World.TryGetChunk(chunkLoader.Position, out var chunk))
                continue;

            if (chunk.State.HasFlag(ChunkState.Generated) && !chunk.State.HasFlag(ChunkState.Meshing) && !chunk.State.HasFlag(ChunkState.Meshed))
            {
                Console.WriteLine($"Enqueuing chunk at {chunkLoader.Position} for meshing.");
                Console.WriteLine(chunk.State);
                Console.WriteLine(World.ChunkLoaders.Length);
                ChunkMesher.EnqueueChunk(chunk);
            }
        }
    }

    void CreateQuadVertexArrayObject()
    {
        if (_quadVertexArrayObject != 0)
            return;

        _quadVertexArrayObject = GL.GenVertexArray();
        var label = "Quad VAO";
        GL.BindVertexArray(_quadVertexArrayObject);
        GL.ObjectLabel(ObjectLabelIdentifier.VertexArray, _quadVertexArrayObject, label.Length, label);
        Span<int> quadVertices = [
            0,  0,  0,
            0,  0,  0,
            0,  0,  0,
            0,  0,  0,
        ];

        var buffer = ShaderBuffer<int>.Create(BufferTarget.ArrayBuffer, BufferUsageHint.StaticDraw, quadVertices);
        buffer.Name = "Quad VBO";

        // quad that will be drawn using instanced triangle_strip rendering
        GL.VertexAttribIPointer(0, 3, VertexAttribIntegerType.Int, 3 * sizeof(int), 0);
        GL.EnableVertexAttribArray(0);
    }

    public void Draw(RenderShader shader)
    {
        shader.Use();
        GL.BindVertexArray(_quadVertexArrayObject);

        shader.SetTextureArray("atlas", _blockTextureAtlas.TextureArray);
        shader.SetVector3("sun", Vector3.UnitY + Vector3.UnitZ);
        shader.SetVector3("ambient", new Vector3(0.75f, 0.75f, 0.75f));
        _faceShaderBuffer.BindToBase(0);
        _materialShaderBuffer.BindToBase(1);
        _chunkInfoShaderBuffer.BindToBase(2);

        if (_meshes.Count == 0)
            return;

        _indirectBuffer.Draw(PrimitiveType.TriangleStrip);
    }
}

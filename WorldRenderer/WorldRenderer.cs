using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using WorldGen.WorldRenderer.Materials;
using WorldGen.WorldRenderer.Mesh;
using WorldGen.ModuleSystem.Storage;
using WorldGen.Renderer;
using WorldGen.Renderer.Shaders;
using WorldGen.Resources.Atlas;
using WorldGen.Resources.Block;
using WorldGen.Universe;
using WorldGen.Utils;

using Vector4 = OpenTK.Mathematics.Vector4;
using System.Collections.Concurrent;
using WorldGen.Universe.PositionTypes;

namespace WorldGen.WorldRenderer;


public class ChunkRenderer
{
    readonly MaterialBuffer _materialBuffer;
    public readonly ChunkMesher ChunkMesher;

    private readonly Atlas _blockTextureAtlas;

    public readonly ShaderBuffer<int> _materialShaderBuffer;
    public readonly ShaderBuffer<int> _faceShaderBuffer;
    public readonly ShaderBuffer<Vector4> _chunkInfoShaderBuffer;
    public readonly IndirectBuffer _indirectBuffer;

    private int _quadVertexArrayObject;

    private readonly RingBuffer<Chunk> Chunks;

    private List<ChunkMesh> _meshes = [];
    public ConcurrentQueue<ChunkPosition> UnloadQueue = [];

    private readonly TrackedQueue<Vector4> _chunkPositions = new();
    private readonly TrackedQueue<int> _faces = new();


    public ChunkRenderer(
        MaterialBuffer materialBuffer,
        Atlas blockTextureAtlas,
        ResourceTypeStorage<Block> blockStorage
    )
    {
        _materialBuffer = materialBuffer;
        _blockTextureAtlas = blockTextureAtlas;
        ChunkMesher = new ChunkMesher(blockStorage, blockTextureAtlas, materialBuffer);

        Chunks = new(20 * 20 * 6);

        _materialShaderBuffer = ShaderBuffer<int>.Create(
            BufferTarget.ShaderStorageBuffer,
            BufferUsageHint.DynamicDraw,
            Math.Max(1, _materialBuffer.EncodedLength)
        );
        _materialShaderBuffer.Name = "MaterialBuffer";

        _chunkInfoShaderBuffer = ShaderBuffer<Vector4>.Create(
            BufferTarget.ShaderStorageBuffer,
            BufferUsageHint.DynamicDraw,
            Chunks.Size
        );
        _chunkInfoShaderBuffer.Name = "ChunkInfoBuffer";

        _faceShaderBuffer = ShaderBuffer<int>.Create(
            BufferTarget.ShaderStorageBuffer,
            BufferUsageHint.DynamicDraw,
            100_000 // Initial size, will grow as needed
        );
        _faceShaderBuffer.Name = "blockFaceBuffer";

        _indirectBuffer = new IndirectBuffer(Chunks.Size);

        UpdateMaterialBuffer(true);

        CreateQuadVertexArrayObject();

        ChunkMesher.StartProcessing();
    }

    public void Clear()
    {
        _chunkInfoShaderBuffer.SetData([]);
        _faceShaderBuffer.SetData([]);
        _indirectBuffer.Clear();
        _materialBuffer.Clear();
        _materialShaderBuffer.SetData([]);
        _chunkPositions.Reset();
        _faces.Reset();
        _meshes.Clear();
    }

    private void UpdateMaterialBuffer(bool force = false)
    {
        if (!force && !_materialBuffer.IsDirty)
            return;

        _materialShaderBuffer.SetData(_materialBuffer.Encode());
        _materialShaderBuffer.Bind();
    }

    private void UpdateFaceBuffer()
    {
        if (_meshes.Count == 0)
            return;

        _chunkPositions.Reset();

        var faceIndex = 0;
        for (var i = 0; i < _meshes.Count; i++)
        {
            var mesh = _meshes[i];

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

    public void AddChunk(Chunk chunk)
    {
        // Remove any existing mesh for this chunk position
        var existingIndex = _meshes.FindIndex(m => m.Position == chunk.Position);
        if (existingIndex >= 0)
        {
            _meshes.RemoveAt(existingIndex);
        }
        Chunks.Enqueue(chunk);
        ChunkMesher.EnqueueChunk(chunk);
    }

    public void RemoveChunk(Chunk chunk)
    {
        if (!Chunks.Has(chunk))
        {
            // Chunk does not exist, nothing to remove
            return;
        }

        UnloadQueue.Enqueue(chunk.Position);
    }

    void UnloadChunks()
    {
        while (UnloadQueue.TryDequeue(out var chunkPosition))
        {
            // Remove the mesh for the chunk
            var existingIndex = _meshes.FindIndex(m => m.Position == chunkPosition);
            if (existingIndex >= 0)
                _meshes.RemoveAt(existingIndex);
        }
    }

    void UpdateMeshes()
    {
        var budget = 5;

        while (budget > 0 && ChunkMesher.TryDequeue(out var chunkMesh))
        {
            if (!Chunks.Has(chunk => chunk.Position == chunkMesh.Position))
                continue; // Skip if the chunk is no longer in the _chunkQueue

            if (chunkMesh.Faces.Count == 0)
                continue; // Skip empty meshes

            _meshes.Add(chunkMesh);
            budget--;
        }

        UpdateFaceBuffer();
    }

    void CreateQuadVertexArrayObject()
    {
        if (_quadVertexArrayObject != 0)
            return;

        _quadVertexArrayObject = GL.GenVertexArray();
        var label = "Quad VAO";
        GL.BindVertexArray(_quadVertexArrayObject);
        GL.ObjectLabel(ObjectLabelIdentifier.VertexArray, _quadVertexArrayObject, label.Length, label);
        int[] quadVertices = [
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

        UnloadChunks();
        UpdateMeshes();

        if (_meshes.Count == 0)
            return;

        _indirectBuffer.Draw(PrimitiveType.TriangleStrip);
    }
}

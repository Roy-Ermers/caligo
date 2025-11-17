using System.ComponentModel.DataAnnotations;
using System.Numerics;
using OpenTK.Graphics.GL;
using OpenTK.Graphics.OpenGL4;
using WorldGen.ChunkRenderer.Materials;
using WorldGen.ChunkRenderer.Mesh;
using WorldGen.ModuleSystem.Storage;
using WorldGen.Renderer;
using WorldGen.Renderer.Shaders;
using WorldGen.Resources.Atlas;
using WorldGen.Resources.Block;
using WorldGen.WorldGenerator;

namespace WorldGen.ChunkRenderer;

struct DrawCall
{
    public Range Faces;
    public Vector3 PositionOffset;
    public int VertexOffset;
}

public class ChunkRenderer
{
    readonly MaterialBuffer _materialBuffer;
    readonly ChunkMesher _chunkMesher;

    private readonly Atlas _blockTextureAtlas;

    public readonly ShaderBuffer<int> _materialShaderBuffer;
    public readonly ShaderBuffer<int> _faceShaderBuffer;

    private int _quadVertexArrayObject;

    private readonly List<Chunk> Chunks = [];

    private readonly List<ChunkMesh> _meshes = [];

    private readonly List<DrawCall> _drawCalls = [];

    public ChunkRenderer(MaterialBuffer materialBuffer, Atlas blockTextureAtlas, ResourceTypeStorage<Block> blockStorage)
    {
        _materialBuffer = materialBuffer;
        _blockTextureAtlas = blockTextureAtlas;
        _chunkMesher = new ChunkMesher(blockStorage, blockTextureAtlas, materialBuffer);

        _materialShaderBuffer = ShaderBuffer<int>.Create(BufferTarget.ShaderStorageBuffer, BufferUsageHint.DynamicRead, Math.Max(1, _materialBuffer.EncodedLength));
        _materialShaderBuffer.Bind();
        _materialShaderBuffer.Name = "MaterialBuffer";

        _faceShaderBuffer = ShaderBuffer<int>.Create(BufferTarget.ShaderStorageBuffer, BufferUsageHint.DynamicRead, 2 * ChunkMesh.MaxFacesPerChunk * 2);
        _faceShaderBuffer.Name = "blockFaceBuffer";

        UpdateMaterialBuffer(true);

        CreateQuadVertexArrayObject();

        _chunkMesher.StartProcessing();
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
        var faces = new List<int>();
        _drawCalls.Clear();

        foreach (var mesh in _meshes)
        {
            foreach (var face in mesh.Faces)
            {
                var startingFaceIndex = faces.Count;
                var faceData = face.Value;
                faces.AddRange(faceData.SelectMany(f => f.Encode()));

                _drawCalls.Add(new DrawCall
                {
                    Faces = startingFaceIndex..faces.Count,
                    PositionOffset = mesh.Position,
                    VertexOffset = (int)face.Key * 4
                });
            }
        }

        _faceShaderBuffer.SetData([.. faces]);
    }

    public void AddChunk(Chunk chunk)
    {
        Chunks.Add(chunk);
        _chunkMesher.EnqueueChunk(chunk);
    }

    void UpdateMeshes()
    {
        _chunkMesher.GetChunkMeshes(_meshes);
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

        var buffer = ShaderBuffer<int>.Create(BufferTarget.ArrayBuffer, BufferUsageHint.StaticRead, quadVertices);
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
        shader.SetVector3("Normal", Vector3.Zero);
        shader.SetMatrix4("model", Matrix4x4.Identity);
        shader.SetVector3("vertexOffset", Vector3.Zero);
        shader.SetVector3("sun", Vector3.UnitY + Vector3.UnitX * 0.5f);
        _faceShaderBuffer.BindToBase(0);
        _materialShaderBuffer.BindToBase(1);

        UpdateMeshes();
        if (_meshes.Count == 0)
            return;
        UpdateMaterialBuffer();
        UpdateFaceBuffer();

        foreach (var drawCall in _drawCalls)
        {
            shader.SetVector3("vertexOffset", drawCall.PositionOffset);
            var (faceOffset, count) = drawCall.Faces.GetOffsetAndLength(int.MaxValue);

            shader.SetInt("faceIndex", faceOffset / 2);

            GL.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, count / 2);
        }
    }
}

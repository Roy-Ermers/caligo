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
using WorldGen.Universe.PositionTypes;
using WorldGen.Universe;

namespace WorldGen.Renderer.Worlds;


public class WorldRenderer
{
	private readonly World _world;
	private readonly MaterialBuffer _materialBuffer;
	public readonly ChunkMesher ChunkMesher;

	private readonly Atlas _blockTextureAtlas;

	public readonly ShaderBuffer<int> MaterialShaderBuffer;
	public readonly ShaderBuffer<int> FaceShaderBuffer;
	public readonly ShaderBuffer<Vector4> ChunkInfoShaderBuffer;
	public readonly IndirectBuffer IndirectBuffer;
	public readonly FaceBuffer FaceBuffer;

	private int _quadVertexArrayObject;

	private readonly Dictionary<ChunkPosition, ChunkMesh> _meshes = [];

    private readonly TrackedQueue<Vector4> _chunkPositions = new();
	private readonly TrackedQueue<int> _faces = new();

	public int RenderDistance { 
		get => FaceBuffer.RenderDistance; 
		set => FaceBuffer.RenderDistance = value; 
	}

	public WorldRenderer(
			World world,
			Atlas blockTextureAtlas,
			ResourceTypeStorage<Block> blockStorage
	)
	{
		_world = world;
		_materialBuffer = new MaterialBuffer();
		_blockTextureAtlas = blockTextureAtlas;
		ChunkMesher = new ChunkMesher(blockStorage, blockTextureAtlas, _materialBuffer);

		FaceBuffer = new FaceBuffer(_materialBuffer);

		CreateQuadVertexArrayObject();

		ChunkMesher.StartProcessing();
	}

	public void Clear()
	{
		ChunkInfoShaderBuffer.SetData([]);
		FaceShaderBuffer.SetData([]);
		IndirectBuffer.Clear();
		_materialBuffer.Clear();
		MaterialShaderBuffer.SetData([]);
		_chunkPositions.Reset();
		_faces.Reset();
		_meshes.Clear();
	}

	public void Update()
	{
		while (ChunkMesher.TryDequeue(out var mesh))
		{
			FaceBuffer.AddMesh(mesh);
		}

		FaceBuffer.Commit();
		
		foreach (var chunkLoader in _world.ChunkLoaders)
		{
			if (!_world.TryGetChunk(chunkLoader.Position, out var chunk))
				continue;

			if (chunk.State.HasFlag(ChunkState.Generated) && !chunk.State.HasFlag(ChunkState.Meshing) && !chunk.State.HasFlag(ChunkState.Meshed))
			{
				ChunkMesher.EnqueueChunk(chunk);
			}
		}
	}

	private void CreateQuadVertexArrayObject()
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
				0,  0,  0
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
		
		FaceBuffer.Render();
	}
}

using System.Numerics;
using Caligo.Client.Graphics;
using Caligo.Client.Graphics.Shaders;
using Caligo.Client.Renderer.Worlds.Materials;
using Caligo.Client.Renderer.Worlds.Mesh;
using Caligo.Client.Resources.Atlas;
using Caligo.Core.ModuleSystem;
using Caligo.Core.ModuleSystem.Storage;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe;
using Caligo.Core.Universe.World;
using Caligo.Core.Utils;
using OpenTK.Graphics.OpenGL;
using Vector4 = OpenTK.Mathematics.Vector4;
using World = Caligo.Core.Universe.World.World;

namespace Caligo.Client.Renderer.Worlds;


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
			ModuleRepository repository,
			ResourceTypeStorage<Block> blockStorage
	)
	{
		_world = world;
		_materialBuffer = new MaterialBuffer();
		ChunkMesher = new ChunkMesher(blockStorage, repository, _materialBuffer);

		_blockTextureAtlas = ChunkMesher.BlockTextureAtlas;

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

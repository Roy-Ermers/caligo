using System.Numerics;
using Caligo.Client.Graphics;
using Caligo.Client.Graphics.Shaders;
using Caligo.Client.Renderer.Worlds.Materials;
using Caligo.Client.Renderer.Worlds.Mesh;
using Caligo.Client.Resources.Atlas;
using Caligo.Core.ModuleSystem;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe;
using Caligo.Core.Universe.World;
using Caligo.Core.Utils;
using Caligo.ModuleSystem;
using Caligo.ModuleSystem.Storage;
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
	
	public readonly FaceBuffer FaceBuffer;

	private int _quadVertexArrayObject;

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
		FaceBuffer.Clear();
	}

	public void Update()
	{
		while (ChunkMesher.TryDequeue(out var mesh))
		{
			FaceBuffer.AddMesh(mesh);
		}

		FaceBuffer.Commit();
		
		foreach (var position in _world.LoadedChunks)
		{
			if (!_world.TryGetChunk(position, out var chunk))
				continue;

			if ((chunk.State & (ChunkState.Generated | ChunkState.Meshing | ChunkState.Meshed)) == ChunkState.Generated)
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
		shader.SetVector3("ambient", new Vector3(0.46f, 0.66f, 0.9f));
		
		FaceBuffer.Render();
	}
}

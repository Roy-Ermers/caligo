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

	private int _quadVertexArrayObject;

	private readonly Dictionary<ChunkPosition, ChunkMesh> _meshes = [];

    private readonly TrackedQueue<Vector4> _chunkPositions = new();
	private readonly TrackedQueue<int> _faces = new();


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

		MaterialShaderBuffer = ShaderBuffer<int>.Create(
				BufferTarget.ShaderStorageBuffer,
				BufferUsageHint.DynamicDraw,
				Math.Max(1, _materialBuffer.EncodedLength)
		);
		MaterialShaderBuffer.Name = "MaterialBuffer";

		ChunkInfoShaderBuffer = ShaderBuffer<Vector4>.Create(
				BufferTarget.ShaderStorageBuffer,
				BufferUsageHint.DynamicDraw,
				1
		);
		ChunkInfoShaderBuffer.Name = "ChunkInfoBuffer";

		FaceShaderBuffer = ShaderBuffer<int>.Create(
				BufferTarget.ShaderStorageBuffer,
				BufferUsageHint.DynamicDraw,
				100_000 // Initial size, will grow as needed
		);
		FaceShaderBuffer.Name = "blockFaceBuffer";

		IndirectBuffer = new IndirectBuffer(100000);

		UpdateMaterialBuffer(true);

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

	private void UpdateMaterialBuffer(bool force = false)
	{
		if (!force && !_materialBuffer.IsDirty)
			return;

		MaterialShaderBuffer.SetData(_materialBuffer.Encode());
	}

	private void UpdateFaceBuffer()
	{
		if (_meshes.Count == 0)
			return;

		_chunkPositions.Reset();
		IndirectBuffer.Clear();

		var faceIndex = 0;
		foreach (var loader in _world.ChunkLoaders)
		{
			if (!_meshes.TryGetValue(loader.Position, out var mesh))
				continue;

			var startingFaceIndex = faceIndex;
			faceIndex += mesh.TotalFaceCount;

			_faces.EnqueueRange(mesh.GetEncodedFaces());

			var (x, y, z) = mesh.Position.ToWorldPosition();
			_chunkPositions.Enqueue(new Vector4(x, y, z, 0));

			IndirectBuffer.Append(new IndirectDrawCommand
			{
				Count = 4,
				InstanceCount = (uint)(faceIndex - startingFaceIndex),
				First = 0,
				BaseInstance = (uint)startingFaceIndex
			});
		}

		if (_chunkPositions.TryUpdate(out var updatedChunkPositions))
			ChunkInfoShaderBuffer.SetData([.. updatedChunkPositions]);

		if (_faces.TryUpdate(out var updatedFaces))
		{
			FaceShaderBuffer.SetData([.. updatedFaces]);
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

		foreach (var chunkLoader in _world.ChunkLoaders)
		{
			if (_meshes.ContainsKey(chunkLoader.Position))
				continue;

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
		FaceShaderBuffer.BindToBase(0);
		MaterialShaderBuffer.BindToBase(1);
		ChunkInfoShaderBuffer.BindToBase(2);

		if (_meshes.Count == 0)
			return;

		IndirectBuffer.Draw(PrimitiveType.TriangleStrip);
	}
}

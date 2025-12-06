using Caligo.Client.Graphics;
using Caligo.Client.Renderer.Worlds.Materials;
using Caligo.Client.Renderer.Worlds.Mesh;
using Caligo.Core.Universe;
using Caligo.Core.Utils;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Caligo.Client.Renderer.Worlds;


public class FaceBuffer: IDisposable
{
	private bool isDirty;
	
	private readonly MaterialBuffer _materialBuffer;

	private readonly ShaderBuffer<int> FaceShaderBuffer;
	private readonly IndirectBuffer IndirectBuffer;
	private readonly ShaderBuffer<int> MaterialShaderBuffer;
	private readonly ShaderBuffer<Vector4> ChunkInfoShaderBuffer;

	private RingBuffer<ChunkMesh> Meshes;

	private readonly Dictionary<ChunkMesh, float> MeshStartTimes = [];

	private int maxChunks => (int)Math.Pow(renderDistance, 3);
	
	private int renderDistance = 10;
	public int RenderDistance
	{
		get => renderDistance;
		set => UpdateRenderDistance(value);
	}

	public FaceBuffer(MaterialBuffer materialBuffer)
	{
		_materialBuffer = materialBuffer;
		MaterialShaderBuffer = ShaderBuffer<int>.Create(
			BufferTarget.ShaderStorageBuffer,
			BufferUsageHint.DynamicDraw,
			Math.Max(1, _materialBuffer.EncodedLength)
		);
		MaterialShaderBuffer.Name = "MaterialBuffer";
		
		FaceShaderBuffer = ShaderBuffer<int>.Create(
			BufferTarget.ShaderStorageBuffer,
			BufferUsageHint.DynamicDraw,
			0
		);
		FaceShaderBuffer.Name = "blockFaceBuffer";
		
		ChunkInfoShaderBuffer = ShaderBuffer<Vector4>.Create(
			BufferTarget.ShaderStorageBuffer,
			BufferUsageHint.DynamicDraw,
			1
		);
		ChunkInfoShaderBuffer.Name = "ChunkInfoBuffer";
		
		IndirectBuffer = new IndirectBuffer(maxChunks);
		

		Meshes = new RingBuffer<ChunkMesh>(maxChunks);
	}

	private void UpdateRenderDistance(int newDistance)
	{
		if (renderDistance == newDistance) return;
		renderDistance = newDistance;
		isDirty = true;

		Meshes = new RingBuffer<ChunkMesh>(maxChunks);
		IndirectBuffer.Resize(maxChunks);
	}
	
	public void AddMesh(ChunkMesh mesh)
	{
		if(mesh.RenderData.Count == 0)
			return;
		
		Meshes.PushBack(mesh);
		isDirty = true;
	}

	public void Commit()
	{
		if (!isDirty && !_materialBuffer.IsDirty)
			return;
		
		List<int> allFaces = [];
		List<Vector4> chunkInfo = [];
		
		IndirectBuffer.Clear();
		isDirty = false;
		
		foreach (var mesh in Meshes)
		{
			var index = allFaces.Count;
			allFaces.AddRange(mesh.RenderData);
			var position = mesh.Position.ToWorldPosition();

			if (!MeshStartTimes.TryGetValue(mesh, out var startTime))
			{
				startTime = (float)Game.Instance.Time;
				MeshStartTimes[mesh] = startTime;
			}
			
			chunkInfo.Add(new Vector4(position.X, position.Y, position.Z, startTime));
			IndirectBuffer.Append(new IndirectDrawCommand
			{
				Count = 4,
				InstanceCount = (uint)mesh.RenderData.Count / BlockFaceRenderData.Size,
				BaseInstance = (uint)index / BlockFaceRenderData.Size
			});
		}
		FaceShaderBuffer.SetData([..allFaces], true);
		ChunkInfoShaderBuffer.SetData([..chunkInfo], true);
		
		if (_materialBuffer.IsDirty)
			MaterialShaderBuffer.SetData(_materialBuffer.Encode());
	}

	public void Clear()
	{
		FaceShaderBuffer.SetData([], true);
		ChunkInfoShaderBuffer.SetData([], true);
		IndirectBuffer.Clear();
		_materialBuffer.Clear();
		Meshes.Clear();
	}
	
	public void Render()
	{
		FaceShaderBuffer.BindToBase(0);
		MaterialShaderBuffer.BindToBase(1);
		ChunkInfoShaderBuffer.BindToBase(2);
		
		IndirectBuffer.Draw(PrimitiveType.TriangleStrip);
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
		FaceShaderBuffer.Dispose();
		MaterialShaderBuffer.Dispose();
		ChunkInfoShaderBuffer.Dispose();
		IndirectBuffer.Dispose();
	}
}
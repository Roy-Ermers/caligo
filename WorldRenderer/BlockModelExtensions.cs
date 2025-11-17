using System.Numerics;
using WorldGen.WorldRenderer.Materials;
using WorldGen.WorldRenderer.Mesh;
using WorldGen.Resources.Atlas;
using WorldGen.Resources.Block;
using WorldGen.Resources.Block.Models;
using WorldGen.Universe.PositionTypes;

namespace WorldGen.WorldRenderer;

public static class BlockModelExtensions
{
    public static BlockFaceRenderData? ToRenderData(
        this BlockModelCube cube,
        Direction direction,
        ChunkLocalPosition chunkPosition,
        Dictionary<string, string> textures,
        MaterialBuffer materialBuffer,
        Atlas atlas
        )
    {
        var face = cube.TextureFaces[direction];

        if (face is null || face.Value.Texture == null)
            return null;


        var textureKey = face.Value.Texture;

        if (textureKey.StartsWith('#'))
            textureKey = textures[textureKey[1..]];

        var textureId = atlas[textureKey];

        var size = Vector3.Abs(cube.To - cube.From);
        ushort Width = (ushort)size.X;
        ushort Height = (ushort)size.Y;
        ushort Depth = (ushort)size.Z;

        var material = new Material()
        {
            Width = Width,
            Height = Height,
            TextureId = textureId,
            UV0 = new Vector2(face.Value.UV.X, face.Value.UV.Y),
            UV1 = new Vector2(face.Value.UV.Z, face.Value.UV.W),
            Tint = face.Value.Tint
        };

        var materialIndex = materialBuffer.Add(material);

        // Compute face center for each direction
        var from = cube.From;
        ushort x = (ushort)(chunkPosition.X * 16);
        ushort y = (ushort)(chunkPosition.Y * 16);
        ushort z = (ushort)(chunkPosition.Z * 16);

        switch (direction)
        {
            case Direction.Up:
                x += (ushort)(from.X + Width);
                y += (ushort)(from.Y + Height);
                z += (ushort)from.Z;
                break;
            case Direction.Down:
                x += (ushort)from.X;
                y += (ushort)from.Y;
                z += (ushort)from.Z;
                break;
            case Direction.North:
                x += (ushort)(from.X + Width);
                y += (ushort)from.Y;
                z += (ushort)from.Z;
                break;
            case Direction.South:
                x += (ushort)from.X;
                y += (ushort)from.Y;
                z += (ushort)(from.Z + Depth);
                break;
            case Direction.West:
                x += (ushort)from.X;
                y += (ushort)from.Y;
                z += (ushort)from.Z;
                break;
            case Direction.East:
                x += (ushort)(from.X + Width);
                y += (ushort)from.Y;
                z += (ushort)(from.Z + Depth);
                break;
        }

        var faceRenderData = new BlockFaceRenderData
        {
            Normal = direction,
            MaterialId = materialIndex,
            X = x,
            Y = y,
            Z = z,
            Light = new Vector4(15, 15, 15, 15) // Assuming full light for simplicity
        };

        return faceRenderData;
    }
}

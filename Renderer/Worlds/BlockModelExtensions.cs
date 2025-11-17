using System.Numerics;
using WorldGen.Renderer.Worlds.Materials;
using WorldGen.Renderer.Worlds.Mesh;
using WorldGen.Resources.Atlas;
using WorldGen.Resources.Block;
using WorldGen.Resources.Block.Models;
using WorldGen.Universe.PositionTypes;
using WorldGen.Utils;

namespace WorldGen.Renderer.Worlds;

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
        var width = (ushort)size.X;
        var height = (ushort)size.Y;
        var depth = (ushort)size.Z;

        var material = new Material()
        {
            Width = width,
            Height = height,
            TextureId = textureId,
            UV0 = new Vector2(face.Value.UV.X, face.Value.UV.Y),
            UV1 = new Vector2(face.Value.UV.Z, face.Value.UV.W),
            Tint = face.Value.Tint
        };

        var materialIndex = materialBuffer.Add(material);

        // Compute face center for each direction
        var from = cube.From;
        var x = (ushort)(chunkPosition.X * 16);
        var y = (ushort)(chunkPosition.Y * 16);
        var z = (ushort)(chunkPosition.Z * 16);

        if (direction == Direction.Up)
        {
            x += (ushort)(from.X + width);
            y += (ushort)(from.Y + height);
            z += (ushort)from.Z;
        }
        else if (direction == Direction.Down)
        {
            x += (ushort)from.X;
            y += (ushort)from.Y;
            z += (ushort)from.Z;
        }
        else if (direction == Direction.North)
        {
            x += (ushort)(from.X + width);
            y += (ushort)from.Y;
            z += (ushort)from.Z;
        }
        else if (direction == Direction.South)
        {
            x += (ushort)from.X;
            y += (ushort)from.Y;
            z += (ushort)(from.Z + depth);
        }
        else if (direction == Direction.West)
        {
            x += (ushort)from.X;
            y += (ushort)from.Y;
            z += (ushort)from.Z;
        }
        else if (direction == Direction.East)
        {
            x += (ushort)(from.X + width);
            y += (ushort)from.Y;
            z += (ushort)(from.Z + depth);
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

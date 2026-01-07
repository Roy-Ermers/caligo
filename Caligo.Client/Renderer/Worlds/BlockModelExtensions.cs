using System.Numerics;
using Caligo.Client.Renderer.Worlds.Materials;
using Caligo.Client.Renderer.Worlds.Mesh;
using Caligo.Client.Resources.Atlas;
using Caligo.Core.Resources.Block.Models;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Utils;
using Random = Caligo.Core.Utils.Random;

namespace Caligo.Client.Renderer.Worlds;

public static class BlockModelExtensions
{
    public static BlockFaceRenderData? ToRenderData(
        this BlockModelCube cube,
        Direction direction,
        ChunkLocalPosition chunkPosition,
        Dictionary<string, string[]> textures,
        MaterialBuffer materialBuffer,
        (short x, short y, short z) offset,
        Atlas atlas,
        Random random
    )
    {
        var face = cube.TextureFaces[direction];

        if (face?.Texture == null)
            return null;

        var textureKey = face.Value.Texture;

        if (textureKey.StartsWith('#'))
        {
            var array = textures[textureKey[1..]];
            if (array.Length == 0)
                throw new Exception($"Texture array '{textureKey}' is empty.");
            textureKey = array[random.Next(0, array.Length - 1)];
        }

        var textureId = atlas[textureKey];

        if (textureId == -1) throw new Exception($"Texture '{textureKey}' not found in atlas.");

        var size = Vector3.Abs(cube.To - cube.From);
        var width = (ushort)size.X;
        var height = (ushort)size.Y;
        var depth = (ushort)size.Z;

        var material = new Material
        {
            Width = width,
            Height = height,
            TextureId = textureId,
            UV0 = new Vector2(face.Value.UV.X, face.Value.UV.Y),
            UV1 = new Vector2(face.Value.UV.Z, face.Value.UV.W),
            Tint = face.Value.Tint,
            Shade = face.Value.Shade
        };


        // Compute face center for each direction
        var from = cube.From;
        var x = (ushort)Math.Clamp(chunkPosition.X * 16 + offset.x, 0, 511);
        var y = (ushort)Math.Clamp(chunkPosition.Y * 16 + offset.y, 0, 511);
        var z = (ushort)Math.Clamp(chunkPosition.Z * 16 + offset.z, 0, 511);

        if (direction == Direction.Up)
        {
            x += (ushort)(from.X + width);
            y += (ushort)(from.Y + height);
            z += (ushort)from.Z;
            material.Height = depth;
        }
        else if (direction == Direction.Down)
        {
            x += (ushort)from.X;
            y += (ushort)from.Y;
            z += (ushort)from.Z;
            material.Height = depth;
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

            material.Width = depth;
        }
        else if (direction == Direction.East)
        {
            x += (ushort)(from.X + width);
            y += (ushort)from.Y;
            z += (ushort)(from.Z + depth);
            material.Width = depth;
        }

        var materialIndex = materialBuffer.Add(material);


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

    public static (ushort x, ushort y, ushort z, ushort width, ushort height) CalculateFacePosition(
        this BlockModelCube cube,
        Direction direction,
        ChunkLocalPosition chunkPosition,
        string offsetType,
        Random random
    )
    {
        var offsetX = (short)((offsetType?.Contains('x') ?? false) ? random.Next(-7, 7) : 0);
        var offsetY = (short)((offsetType?.Contains('y') ?? false) ? random.Next(-7, 7) : 0);
        var offsetZ = (short)((offsetType?.Contains('z') ?? false) ? random.Next(-7, 7) : 0);


        var from = cube.From;
        var size = cube.Size;

        var x = Math.Clamp(chunkPosition.X * 16 + offsetX, 0, 511);
        var y = Math.Clamp(chunkPosition.Y * 16 + offsetY, 0, 511);
        var z = Math.Clamp(chunkPosition.Z * 16 + offsetZ, 0, 511);

        var (finalX, finalY, finalZ, finalWidth, finalHeight) = direction switch
        {
            Direction.Up => (
                x + from.X + size.X,
                y + from.Y + size.Y,
                z + from.Z,
                size.Z,
                size.X
            ),
            Direction.Down => (
                x + from.X,
                y + from.Y,
                z + from.Z,
                size.X,
                size.Z
            ),
            Direction.North => (
                x + from.X + size.X,
                y + from.Y,
                z + from.Z,
                size.X,
                size.Y
            ),
            Direction.South => (
                x + from.X,
                y + from.Y,
                z + from.Z + size.Z,
                size.X,
                size.Y
            ),
            Direction.West => (
                x + from.X,
                y + from.Y,
                z + from.Z,
                size.Z,
                size.Y
            ),
            Direction.East => (
                x + from.X + size.X,
                y + from.Y,
                z + from.Z + size.Z,
                size.Z,
                size.Y
            ),
            _ => (x, y, z, size.X, size.Y)
        };

        return ((ushort)finalX, (ushort)finalY, (ushort)finalZ, (ushort)finalWidth, (ushort)finalHeight);
    }
}
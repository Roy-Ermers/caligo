using System.Numerics;
using Caligo.Client.Renderer.Worlds.Materials;
using Caligo.Client.Renderer.Worlds.Mesh;
using Caligo.Client.Resources.Atlas;
using Caligo.Core.Resources.Block.Models;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Utils;

namespace Caligo.Client.Renderer.Worlds;

public static class BlockModelExtensions
{
    public static BlockFaceRenderData? ToRenderData(
        this BlockModelCube cube,
        Direction direction,
        ChunkLocalPosition chunkPosition,
        Dictionary<string, string> textures,
        MaterialBuffer materialBuffer,
        (short x, short y, short z) offset,
        Atlas atlas
        )
    {
        var face = cube.TextureFaces[direction];

        if (face?.Texture == null)
            return null;

        var textureKey = face.Value.Texture;

        if (textureKey.StartsWith('#'))
            textureKey = textures[textureKey[1..]];

        var textureId = atlas[textureKey];
        
        if(textureId == -1) 
        {
            throw new Exception($"Texture '{textureKey}' not found in atlas.");
        }

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
            Tint = face.Value.Tint,
            Shade = face.Value.Shade,
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
}

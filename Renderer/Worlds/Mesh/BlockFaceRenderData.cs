using System.Numerics;
using WorldGen.Resources.Block;

namespace WorldGen.Renderer.Worlds.Mesh;

public record struct BlockFaceRenderData
{
    /// <summary>
    /// The normal direction of the face.
    /// </summary>
    /// <remarks>
    /// Excluded from encoding.
    /// </remarks>
    public Direction Normal;

    /// <summary>
    /// The face position in the chunk.
    /// </summary>
    /// <remarks>
    /// ranges from 0 to 255
    /// </remarks>
    public ushort X;
    /// <summary>
    /// The face position in the chunk.
    /// </summary>
    /// <remarks>
    /// ranges from 0 to 255
    /// </remarks>
    public ushort Y;
    /// <summary>
    /// The face position in the chunk.
    /// </summary>
    /// <remarks>
    /// ranges from 0 to 255
    /// </remarks>
    public ushort Z;

    /// <summary>
    /// The light value of the face.
    /// </summary>
    /// <remarks>
    /// The light value is a vector with 4 components:
    /// - X: Red light value (0-15)
    /// - Y: Green light value (0-15)
    /// - Z: Blue light value (0-15)
    /// - W: Alpha value (0-15)
    /// </summary>
    public Vector4 Light;

    /// <summary>
    /// The material ID of the face.
    /// </summary>
    /// <remarks>
    /// The material ID is an index into a material buffer, which contains the texture and properties of the face.
    /// </remarks>
    /// <remarks>
    /// Can contain values from 0 to 65535 (0xFFFF).
    /// </remarks>
    public int MaterialId;

    public readonly int[] Encode()
    {
        // if (X >= 255 || Y > 255 || Z > 255)
        //     throw new ArgumentOutOfRangeException("X, Y, and Z must be in the range of 0 to 255.");

        int x = X & 0x1FF;
        int y = Y & 0x1FF;
        int z = Z & 0x1FF;
        int Position = 0;
        Position |= x << 0;
        Position |= y << 9;
        Position |= z << 18;
        Position |= ((int)Normal & 0x7) << 27; // Normal direction as an int (0-5)

        int visualData = 0;
        int lightX = (int)Light.X & 0x0F;
        int lightY = (int)Light.Y & 0x0F;
        int lightZ = (int)Light.Z & 0x0F;
        int lightA = (int)Light.W & 0x0F;
        int materialId = MaterialId & 0xFFFF;
        visualData |= lightX << 0; // Red light value
        visualData |= lightY << 4; // Green light value
        visualData |= lightZ << 8; // Blue light value
        visualData |= lightA << 12; // Alpha value
        visualData |= materialId << 16; // Material ID

        return [Position, visualData];
    }

    public static BlockFaceRenderData Decode(IEnumerable<int> data)
    {
        if (data.Count() != 2)
            throw new ArgumentException("Data must contain exactly two integers.");

        int position = data.First();
        int visualData = data.Skip(1).First();

        int x = position & 0xFF;
        int y = (position >> 8) & 0xFF;
        int z = (position >> 16) & 0xFF;
        Direction normal = (Direction)(position >> 24);

        int lightX = visualData & 0x0F;
        int lightY = (visualData >> 4) & 0x0F;
        int lightZ = (visualData >> 8) & 0x0F;
        int lightA = (visualData >> 12) & 0x0F;
        int materialId = (visualData >> 16) & 0xFFFF;

        return new BlockFaceRenderData
        {
            X = (ushort)x,
            Y = (ushort)y,
            Z = (ushort)z,
            Normal = normal,
            Light = new Vector4(lightX, lightY, lightZ, lightA),
            MaterialId = materialId
        };
    }
}

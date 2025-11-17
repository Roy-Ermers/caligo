using WorldGen.Utils;

namespace WorldGen.Resources.Block.Models;

public struct TextureFaces
{
    public BlockFace? Down;
    public BlockFace? Up;
    public BlockFace? North;
    public BlockFace? South;
    public BlockFace? West;
    public BlockFace? East;

    public TextureFaces SetTextureVariables(Dictionary<string, string> textures)
    {
        return new TextureFaces
        {
            Down = SetTextureVariable(Down, textures),
            Up = SetTextureVariable(Up, textures),
            North = SetTextureVariable(North, textures),
            South = SetTextureVariable(South, textures),
            West = SetTextureVariable(West, textures),
            East = SetTextureVariable(East, textures),
        };
    }

    private static BlockFace? SetTextureVariable(BlockFace? face, Dictionary<string, string> textures)
    {
        if (face is null)
            return null;

        if (face.Value.TextureVariable == null)
            return face.Value;

        if (!textures.TryGetValue(face.Value.TextureVariable, out var value))
            return face.Value;

        var updatedFace = face.Value.Clone();
        updatedFace.Texture = value;
        face = updatedFace;

        return face;
    }

    public readonly BlockFace? this[Direction direction] => direction switch
    {
        Direction.Down => Down,
        Direction.Up => Up,
        Direction.North => North,
        Direction.South => South,
        Direction.West => West,
        Direction.East => East,
        _ => null
    };
}

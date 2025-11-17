using WorldGen.Resources.Block.Models;

namespace WorldGen.Resources.Block;

public record class Block
{
    public ushort NumericId = 0;
    public required string Name;

    public Dictionary<string, string> Textures = [];

    public string? ModelName;
    public BlockModel? Model;
}

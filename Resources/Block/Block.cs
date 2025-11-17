using WorldGen.Resources.Block.Models;

namespace WorldGen.Resources.Block;

public struct BlockVariant
{
    public string ModelName;
    public BlockModel Model;
    public int Weight;
    public Dictionary<string, string> Textures;
}

public record class Block
{
    public ushort NumericId = 0;
    public required string Name;

    public BlockVariant[] Variants = [];
}

using WorldGen.Resources.Block.Models;

namespace WorldGen.Resources.Block;

public struct BlockVariantData
{
    public BlockModelLink Model;
    public int? Weight;
}

public struct ModuleBlockData
{
    public BlockModelLink? Model;
    public BlockVariantData[] Variants;
}

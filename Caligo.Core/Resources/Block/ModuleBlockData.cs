namespace Caligo.Core.Resources.Block;

public struct BlockVariantData
{
    public BlockModelLink Model;
    public int? Weight;
}

public struct ModuleBlockData
{
    public BlockModelLink? Model;
    public bool IsSolid;
    public BlockVariantData[] Variants;
}
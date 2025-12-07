using System.Text.Json;
using Caligo.Core.FileSystem.Json;
using Caligo.Core.Resources.Block;
using Caligo.Core.Resources.Block.Models;
using Caligo.ModuleSystem;
using Caligo.ModuleSystem.Importers;
using Caligo.ModuleSystem.Storage;

namespace Caligo.Core.ModuleSystem.Importers.Blocks;

public class BlockImporter : IImporter, IResourceProcessor
{

    private static BlockVariant[] ConvertToBlockVariants(BlockVariantData[] variants)
    {
        var blockVariants = new List<BlockVariant>();
        foreach (var variant in variants)
        {
            var blockVariant = new BlockVariant
            {
                ModelName = variant.Model.BlockModelName ?? "",
                Model = null!,
                Weight = variant.Weight ?? 1,
                Textures = variant.Model.Textures ?? []
            };
            blockVariants.Add(blockVariant);
        }
        return [.. blockVariants];
    }

    public void Import(Module module)
    {
        var rootDirectory = Path.Join(module.AbsoluteDirectory, "blocks");
        if (!Directory.Exists(rootDirectory))
            return;

        var files = Directory.EnumerateFiles(rootDirectory, "*.json", SearchOption.AllDirectories);
        var blockStorage = module.GetStorage<Block>();
        
        blockStorage.Add(Block.Air.Name, Block.Air);

        foreach (var file in files)
        {
            try
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var blockName = Identifier.Create(module.Identifier, name);

                var jsonContent = File.ReadAllText(file);
                var blockData = JsonSerializer.Deserialize<ModuleBlockData>(jsonContent, JsonOptions.SerializerOptions);
                if (blockData.Model is not null)
                {
                    blockData.Variants = [
                           new BlockVariantData {
                            Model = blockData.Model.Value,
                            Weight = 1
                        }
                    ];
                }

                if (blockData.Variants.Length == 0)
                {
                    Console.WriteLine($"Block {blockName} has no model or variants defined.");
                    continue;
                }

                Block block = new()
                {
                    Name = blockName,
                    IsSolid = blockData.IsSolid,
                    Variants = ConvertToBlockVariants(blockData.Variants)
                };

                blockStorage.Add(blockName, block);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error importing block: {file}");
                Console.WriteLine(e.Message);
            }
        }
    }

    public void Process(ResourceStorage storage)
    {
        var blockStorage = storage.GetStorage<Block>();

        var Air = Identifier.Resolve("air");
        ushort index = 0;
        
        if (blockStorage.Count == 1)
            return;

        foreach (var (identifier, block) in blockStorage)
        {
            block.NumericId = index++;

            for (var variantIndex = 0; variantIndex < block.Variants.Length; variantIndex++)
            {
                var variant = block.Variants[variantIndex];

                if (variant.ModelName is null)
                    continue;

                if (!storage.TryGet<BlockModel>(variant.ModelName, out var model))
                {
                    Console.WriteLine($"Blockmodel {variant.ModelName} for {identifier} not found.");
                    continue;
                }

                variant.Model = model;

                block.Variants[variantIndex] = variant;
            }
        }
    }
}

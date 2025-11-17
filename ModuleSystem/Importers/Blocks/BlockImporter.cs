using System.Text.Json;
using WorldGen.FileSystem.Json;
using WorldGen.ModuleSystem.Storage;
using WorldGen.Renderer.Shaders;
using WorldGen.Resources.Block;
using WorldGen.Resources.Block.Models;

namespace WorldGen.ModuleSystem.Importers.Blocks;

public class BlockImporter : IImporter, IResourceProcessor
{
    public void Import(Module module)
    {
        var rootDirectory = Path.Join(module.AbsoluteDirectory, "blocks");
        if (!Directory.Exists(rootDirectory))
            return;

        var files = Directory.EnumerateFiles(rootDirectory, "*.json", SearchOption.AllDirectories);
        var blockStorage = module.GetStorage<Block>();

        foreach (var file in files)
        {
            try
            {

                var name = Path.GetFileNameWithoutExtension(file);
                var blockName = Identifier.Create(module.Identifier, name);

                Block block = new() { Name = blockName };

                var jsonContent = File.ReadAllText(file);
                var blockData = JsonSerializer.Deserialize<ModuleBlockData>(jsonContent, JsonOptions.SerializerOptions);
                // Copy properties from blockData to block
                if (blockData.Model.BlockModelName is not null)
                {
                    block.Textures = blockData.Model.Textures;
                    block.ModelName = blockData.Model.BlockModelName;
                }

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

        blockStorage.Prepend(Air, new Block
        { Name = Air });

        if (blockStorage.Count == 1)
            return;

        foreach (var (identifier, block) in blockStorage)
        {
            block.NumericId = index++;
            if (block.ModelName is null)
                continue;

            if (!storage.TryGet<BlockModel>(block.ModelName, out var model))
            {
                Console.WriteLine($"Blockmodel {block.ModelName} for {identifier} not found.");
                continue;
            }

            block.Model = model;
        }
    }
}

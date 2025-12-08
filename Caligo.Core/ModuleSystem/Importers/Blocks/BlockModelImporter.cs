using System.Text.Json;
using Caligo.Core.FileSystem.Json;
using Caligo.Core.Resources.Block.Models;
using Caligo.ModuleSystem;
using Caligo.ModuleSystem.Importers;
using Caligo.ModuleSystem.Storage;

namespace Caligo.Core.ModuleSystem.Importers.Blocks;

public class BlockModelImporter : IImporter, IResourceProcessor
{
    public void Import(Module module)
    {
        var rootDirectory = Path.Join(module.AbsoluteDirectory, "blockmodels");
        if (!Directory.Exists(rootDirectory))
            return;

        var files = Directory.EnumerateFiles(rootDirectory, "*.json", SearchOption.AllDirectories);
        var blockStorage = module.GetStorage<BlockModel>();

        foreach (var file in files)
            try
            {
                // the relative path without extension is used as the identifier
                var name = Path.ChangeExtension(Path.GetRelativePath(rootDirectory, file), null);
                var blockModelName = Identifier.Create(module.Identifier, name);
                var model = JsonSerializer.Deserialize<BlockModel>(File.ReadAllText(file),
                    JsonOptions.SerializerOptions);

                if (model is null)
                    throw new InvalidOperationException($"Failed to deserialize block model from file: {file}");

                model.Name = blockModelName;

                blockStorage.Add(blockModelName, model);

                Console.WriteLine($"Importing block model: {blockModelName}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error importing block model: {file}");
                Console.WriteLine(e.Message);
            }
    }

    public void Process(ResourceStorage storage)
    {
        var blockModelStorage = storage.GetStorage<BlockModel>();
        if (blockModelStorage.Count == 0)
            return;

        foreach (var (identifier, blockModel) in blockModelStorage)
        {
            if (blockModel.ParentName is null)
                continue;
            var parentIdentifier = Identifier.Resolve(blockModel.ParentName);

            if (!blockModelStorage.TryGetValue(parentIdentifier, out var parentModel))
            {
                Console.WriteLine($"Parent model {blockModel.ParentName} for {identifier} not found.");
                continue;
            }

            blockModel.Parent = parentModel;
        }

        // Collapse all block models to resolve inheritance
        foreach (var (_, blockModel) in blockModelStorage) blockModel.Build();

        Console.WriteLine($"Processed {blockModelStorage.Count} block models.");
    }
}
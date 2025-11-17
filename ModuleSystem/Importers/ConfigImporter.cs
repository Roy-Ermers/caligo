using System.Text.Json;
using System.Text.Json.Nodes;
using WorldGen.ModuleSystem.Storage;

namespace WorldGen.ModuleSystem.Importers;

public class ConfigImporter : IImporter
{

    public void Import(Module module)
    {
        var configFile = Path.Join(module.AbsoluteDirectory, "config.json");
        if (!File.Exists(configFile))
            return;

        var configStorage = module.GetStorage<string>("Config");

        var node = JsonNode.Parse(File.ReadAllText(configFile), null, new()
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        if (node is null)
        {
            Console.WriteLine($"Error parsing config file: {configFile}");
            return;
        }

        ParseJsonNode(node, configStorage);
        Console.WriteLine($"Imported config from {configFile} into {module.Identifier}");
    }


    private static void ParseJsonNode(JsonNode node, ResourceTypeStorage<string> configStorage)
    {
        var kind = node.GetValueKind();
        var currentPath = node.GetPath();

        if (currentPath.StartsWith('$'))
        {
            if (currentPath.Length == 1)
                currentPath = "";
            else
                currentPath = currentPath[2..];
        }

        if (kind != JsonValueKind.Object)
        {
            configStorage.Add(currentPath, node.ToJsonString().Trim('"'));
            return;
        }

        foreach (var property in node.AsObject())
        {
            var propertyValue = property.Value;
            if (propertyValue is null)
                continue;

            ParseJsonNode(propertyValue, configStorage);
        }
    }
}

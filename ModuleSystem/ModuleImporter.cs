using WorldGen.ModuleSystem.Importers;
using WorldGen.ModuleSystem.Storage;

namespace WorldGen.ModuleSystem;

public class ModuleImporter
{
    private readonly List<IImporter> _importers = [];

    public ModuleImporter AddImporter<T>() where T : IImporter, new()
    {
        _importers.Add(new T());
        return this;
    }

    public Module Load(string absolutePath, string identifier)
    {
        Console.WriteLine($"Loading module {identifier} from {absolutePath}");

        var module = new Module(identifier, absolutePath);

        if (!Directory.Exists(absolutePath))
            throw new DirectoryNotFoundException($"The directory {absolutePath} does not exist.");

        foreach (var importerType in _importers)
        {
            importerType.Import(module);
        }

        module.Clean();

        return module;
    }

    public void Process(ResourceStorage storage)
    {
        foreach (var importer in _importers)
        {
            if (importer is IResourceProcessor resourceProcessor)
                resourceProcessor.Process(storage);
        }
    }
}

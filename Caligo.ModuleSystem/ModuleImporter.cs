using Caligo.ModuleSystem.Importers;
using Caligo.ModuleSystem.Runtime;

namespace Caligo.ModuleSystem;

public class ModuleImporter
{
    private readonly List<IImporter> _importers = [];
    private readonly ModuleRepository _repository;

    public ModuleImporter(ModuleRepository repository)
    {
        _repository = repository;
    }

    public ModuleImporter AddImporter<T>() where T : IImporter, new()
    {
        _importers.Add(new T());
        return this;
    }
    
    /// <summary>
    /// Adds a new module to the container.
    /// </summary>
    /// <param name="directory">Where to find the module</param>
    /// <exception cref="DuplicateNameException">Thrown if a module with the same identifier already exists.</exception>
    private void LoadModule(string directory)
    {
        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), directory);
        var identifier = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar)) ?? throw new ArgumentException("Directory name cannot be null or empty.", nameof(directory));
        Console.WriteLine($"Loading module {identifier} from {absolutePath}");

        Module module;
        if (File.Exists(Path.Combine(absolutePath, "module.js")))
            module = new JsModule(identifier, absolutePath);
        else
            module = LoadStaticModule(identifier, absolutePath);

        _repository.AddModule(module);
    }

    private Module LoadStaticModule(string identifier, string absolutePath)
    {
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
    
    public void Load(string directory)
    {
        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), directory);
        if (!Directory.Exists(absolutePath))
            throw new DirectoryNotFoundException($"The directory {absolutePath} does not exist.");

        var directories = Directory.GetDirectories(absolutePath);
        
        foreach (var dir in directories)
            LoadModule(dir);
        
        Process();
    }

    private void Process()
    {
        foreach (var importer in _importers)
        {
            if (importer is IResourceProcessor resourceProcessor)
                resourceProcessor.Process(_repository.Resources);
        }
    }
}

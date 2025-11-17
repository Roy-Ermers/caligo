using System.Data;
using System.Diagnostics.CodeAnalysis;
using Caligo.Core.ModuleSystem.Storage;

namespace Caligo.Core.ModuleSystem;

public class ModuleRepository
{
    public static ModuleRepository Current { get; protected set; } = null!;
    private readonly Dictionary<string, Module> _modules = [];
    private readonly ModuleImporter importer;

    /// <summary>
    /// Gets an array of all modules currently loaded in the container.
    /// </summary>
    public Module[] Modules => [.. _modules.Values];

    public ResourceStorage Resources { get; private set; } = new();

    public ModuleRepository(ModuleImporter importer)
    {
        this.importer = importer ?? throw new ArgumentNullException(nameof(importer));
        Current = this;
    }

    public void LoadModules(string directory)
    {
        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), directory);
        if (!Directory.Exists(absolutePath))
            throw new DirectoryNotFoundException($"The directory {absolutePath} does not exist.");

        var directories = Directory.GetDirectories(absolutePath);
        foreach (var dir in directories)
            LoadModule(dir);
    }

    /// <summary>
    /// Adds a new module to the container.
    /// </summary>
    /// <param name="directory">Where to find the module</param>
    /// <exception cref="DuplicateNameException">Thrown if a module with the same identifier already exists.</exception>
    public void LoadModule(string directory)
    {
        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), directory);
        var identifier = Path.GetFileName(directory.TrimEnd(Path.DirectorySeparatorChar)) ?? throw new ArgumentException("Directory name cannot be null or empty.", nameof(directory));
        LoadModule(absolutePath, identifier);
    }

    /// <summary>
    /// Adds a new module to the container.
    /// </summary>
    /// <param name="directory">Where to find the module</param>
    /// <param name="identifier">The identifier of the module</param>
    /// <exception cref="DuplicateNameException">Thrown if a module with the same identifier already exists.</exception>
    public void LoadModule(string directory, string identifier)
    {
        if (_modules.ContainsKey(identifier))
            throw new DuplicateNameException($"Module with identifier {identifier} already exists");

        var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), directory);
        var module = importer.Load(absolutePath, identifier);

        _modules.Add(module.Identifier, module);
        Resources.Import(module.Identifier, module);
    }

    /// <summary>
    /// Retrieves a module by its identifier.
    /// </summary>
    /// <param name="moduleName">The identifier of the module to retrieve.</param>
    /// <returns>The requested module.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no module with the specified identifier exists.</exception>
    public Module GetModule(string moduleName)
    {
        if (!_modules.TryGetValue(moduleName, out var value))
            throw new KeyNotFoundException($"Module with identifier {moduleName} does not exist");

        return value;
    }

    public ResourceTypeStorage<T> GetAll<T>() where T : class
    {
        return Resources.GetStorage<T>();
    }

    public ResourceTypeStorage<T> GetAll<T>(string storageKey) where T : class
    {
        return Resources.GetStorage<T>(storageKey);
    }

    public T Get<T>(string identifier) where T : class =>
        Resources.Get<T>(identifier);

    public bool TryGet<T>(string identifier, [MaybeNullWhen(false)] out T? result) where T : class =>
        Resources.TryGet<T>(identifier, out result);

    public void Build()
    {
        importer.Process(Resources);
    }
}

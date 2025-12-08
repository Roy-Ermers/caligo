using System.Data;
using System.Diagnostics.CodeAnalysis;
using Caligo.ModuleSystem.Storage;

namespace Caligo.ModuleSystem;

public class ModuleRepository
{
    private readonly Dictionary<string, Module> _modules = [];

    public ModuleRepository()
    {
        Current = this;
    }

    public static ModuleRepository Current { get; internal set; } = null!;

    /// <summary>
    ///     Gets an array of all modules currently loaded in the container.
    /// </summary>
    public Module[] Modules => [.. _modules.Values];

    /// <summary>
    ///     The resource storage for all modules.
    /// </summary>
    public ResourceStorage Resources { get; } = new();

    /// <summary>
    ///     Adds a new module to the container.
    /// </summary>
    /// <param name="module">The module to add.</param>
    /// <exception cref="DuplicateNameException">Thrown when the namespace is already taken</exception>
    public void AddModule(Module module)
    {
        if (!_modules.TryAdd(module.Identifier, module))
            throw new DuplicateNameException($"Module with identifier {module.Identifier} already exists");

        Resources.Import(module.Identifier, module);
    }

    /// <summary>
    ///     Retrieves a module by its identifier.
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

    public bool TryGetModule(string moduleName, [NotNullWhen(true)] out Module? module)
    {
        return _modules.TryGetValue(moduleName, out module);
    }

    public ResourceTypeStorage<T> GetAll<T>() where T : class
    {
        return Resources.GetStorage<T>();
    }

    public ResourceTypeStorage<T> GetAll<T>(string storageKey) where T : class
    {
        return Resources.GetStorage<T>(storageKey);
    }

    public T Get<T>(string identifier) where T : class
    {
        return Resources.Get<T>(identifier);
    }

    public bool TryGet<T>(string identifier, [NotNullWhen(true)] out T? result) where T : class
    {
        return Resources.TryGet(identifier, out result);
    }
}
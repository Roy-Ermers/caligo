using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace WorldGen.ModuleSystem.Storage;

public class ResourceStorage
{
    private readonly Dictionary<string, BaseResourceTypeStorage> storages = [];
    /// <summary>
    /// A collection of all resource storages in this storage.
    /// </summary>
    public FrozenDictionary<string, BaseResourceTypeStorage> Storages => storages.ToFrozenDictionary();

    /// <summary>
    /// Checks if a storage for the given type exists.
    /// </summary>
    /// <typeparam name="T">The type to search for</typeparam>
    /// <returns>If the storage exists.</returns>
    public bool HasStorage<T>() where T : class => storages.ContainsKey(typeof(T).Name);
    /// <summary>
    /// Checks if a storage for the given key exists.
    /// </summary>
    /// <param name="key">The key to search for</param>
    /// <returns>If the storage exists.</returns>
    public bool HasStorage(string key) => storages.ContainsKey(key);

    /// <summary>
    /// Tries to get a storage for the given type.
    /// </summary>
    /// <typeparam name="T">The type to search for</typeparam>
    /// <param name="storage">Storage when found</param>
    /// <returns>If the storage exists or not</returns>
    public bool TryGetStorage<T>([NotNullWhen(true)] out ResourceTypeStorage<T>? storage) where T : class
        => TryGetStorage(typeof(T).Name, out storage);

    /// <summary>
    /// Tries to get a storage for the given key.
    /// </summary>
    /// <param name="key">The key to search for</param>
    /// <param name="storage">Storage when found</param>
    /// <returns>If the storage exists or not</returns>
    public bool TryGetStorage<T>(string key, [NotNullWhen(true)] out ResourceTypeStorage<T>? storage) where T : class
    {
        if (storages.TryGetValue(key, out var storageBase))
        {
            storage = (ResourceTypeStorage<T>)storageBase;
            return true;
        }

        storage = null;
        return false;
    }

    /// <summary>
    /// Gets or creates a storage for the given type.
    /// </summary>
    /// <typeparam name="T">Type to get storage for.</typeparam>
    /// <returns>Storage to use</returns>
    public ResourceTypeStorage<T> GetStorage<T>() where T : class => GetStorage<T>(typeof(T).Name);

    /// <summary>
    /// Gets or creates a storage for the given type.
    /// </summary>
    /// <param name="key">Key to get storage for.</param>
    /// <returns>Storage to use</returns>
    public ResourceTypeStorage<T> GetStorage<T>(string key) where T : class
    {
        if (storages.TryGetValue(key, out var storage))
        {
            return (ResourceTypeStorage<T>)storage;
        }

        var newStorage = new ResourceTypeStorage<T>(key);
        storages.Add(newStorage.Key, newStorage);
        return newStorage;
    }

    public void Import(string Namespace, ResourceStorage other)
    {
        ArgumentNullException.ThrowIfNull(other);

        foreach (var (key, storage) in other.storages)
        {
            if (!storages.TryGetValue(key, out var existingStorage))
                storages.Add(key, storage);
            else
                existingStorage.Import(Namespace, storage);
        }
    }

    public T Get<T>(string identifier) where T : class
    {
        var key = Identifier.Resolve(identifier);

        if (!TryGetStorage<T>(out var storage))
            throw new KeyNotFoundException($"{typeof(T).Name} with identifier {identifier} not found");

        return storage.Get(key);
    }

    public bool TryGet<T>(string identifier, [MaybeNullWhen(false)] out T? result) where T : class
    {
        var key = Identifier.Resolve(identifier);

        if (!TryGetStorage<T>(out var storage))
        {
            result = null;
            return false;
        }

        return storage.TryGetValue(key, out result);
    }

    /// <summary>
    /// Build the cache of storages.
    /// This will remove all storages that have no entries.
    /// It should be called after all storages have been filled.
    /// </summary>
    public void Clean()
    {
        foreach (var (key, storage) in storages)
        {
            if (storage.Count == 0)
                storages.Remove(key);
        }
    }
}

using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace WorldGen.ModuleSystem.Storage;

public abstract class BaseResourceTypeStorage
{
    public string Key;
    public abstract Type Type { get; }
    public abstract int Count { get; }

    public BaseResourceTypeStorage()
    {
        Key = Type.Name;
    }

    public BaseResourceTypeStorage(string key)
    {
        Key = key;
    }

    public abstract void Import(string Namespace, BaseResourceTypeStorage other);
}

public class ResourceTypeStorage<T> : BaseResourceTypeStorage, IEnumerable<KeyValuePair<string, T>> where T : class
{
    public override Type Type => typeof(T);
    private Dictionary<string, T> Storage = [];
    public override int Count => Storage.Count;

    public ResourceTypeStorage() : base() { }

    public ResourceTypeStorage(string key) : base(key) { }

    public T this[string key]
    {
        get => Get(key);
        set => Add(key, value);
    }

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= Storage.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            return Storage.ElementAt(index).Value;
        }
        set
        {
            if (index < 0 || index >= Storage.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            var key = Storage.ElementAt(index).Key;
            Storage[key] = value;
        }
    }

    public void Prepend(string key, T value)
    {
        Storage = Storage.Prepend(new KeyValuePair<string, T>(key, value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void Add(string key, T value)
    {
        Storage.Add(key, value);
    }

    /// <summary>
    /// Imports the contents of another storage into this one, using the specified namespace as a prefix for the keys.
    /// If the other storage is of a different type, an InvalidOperationException will be thrown.
    /// </summary>
    /// <param name="Namespace"></param>
    /// <param name="other"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public override void Import(string Namespace, BaseResourceTypeStorage other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other is not ResourceTypeStorage<T> otherStorage)
            throw new InvalidOperationException($"Cannot import {other.Type.Name} into {Type.Name}");

        Import(Namespace, otherStorage);
    }

    /// <summary>
    /// Imports the contents of another storage into this one, using the specified namespace as a prefix for the keys.
    /// </summary>
    /// <param name="Namespace">The namespace to use as a prefix for the keys.</param>
    /// <param name="other">The other storage to import from.</param>
    public void Import(string Namespace, ResourceTypeStorage<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        foreach (var kvp in other.Storage)
        {
            var key = Identifier.Resolve(kvp.Key, Namespace);
            Storage[key] = kvp.Value;
        }
    }

    public T Get(string key)
    {
        return Storage[key];
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out T value)
    {
        return Storage.TryGetValue(key, out value);
    }

    public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
    {
        return Storage.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public static class ModuleResourceStorageExtensions
{
    public static IEnumerable<KeyValuePair<string, object>> CastToObjectEnumerable(this BaseResourceTypeStorage storage)
    {
        var enumerator = ((IEnumerable)storage).GetEnumerator();
        while (enumerator.MoveNext())
        {
            var kvp = enumerator.Current;
            var keyProp = kvp.GetType().GetProperty("Key");
            var valueProp = kvp.GetType().GetProperty("Value");
            var key = (string?)keyProp?.GetValue(kvp);
            var value = valueProp?.GetValue(kvp);
            if (key is null || value is null)
                continue;
            yield return new KeyValuePair<string, object>(key, value);
        }
    }
}
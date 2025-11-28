using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Caligo.ModuleSystem.Storage;

public abstract class BaseResourceTypeStorage
{
    public string Key;
    public abstract Type Type { get; }
    public abstract int Count { get; }

    public BaseResourceTypeStorage()
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        Key = Type.Name;
    }

    public BaseResourceTypeStorage(string key)
    {
        Key = key;
    }

    public abstract void Import(string @namespace, BaseResourceTypeStorage other);
}

public class ResourceTypeStorage<T>(string key) : BaseResourceTypeStorage(key), IEnumerable<KeyValuePair<string, T>> where T : class
{
    public override Type Type => typeof(T);
    private SortedList<string, T> _storage = [];
    public override int Count => _storage.Count;

    public T this[string key]
    {
        get => Get(key);
        set => Add(key, value);
    }

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= _storage.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            return _storage.Values[index];
        }
        set
        {
            if (index < 0 || index >= _storage.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            var key = _storage.Keys[index];
            _storage[key] = value;
        }
    }

    public void Add(string key, T value)
    {
        _storage.Add(key, value);
    }

    /// <summary>
    /// Imports the contents of another storage into this one, using the specified namespace as a prefix for the keys.
    /// If the other storage is of a different type, an InvalidOperationException will be thrown.
    /// </summary>
    /// <param name="namespace"></param>
    /// <param name="other"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public override void Import(string @namespace, BaseResourceTypeStorage other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other is not ResourceTypeStorage<T> otherStorage)
            throw new InvalidOperationException($"Cannot import {other.Type.Name} into {Type.Name}");

        Import(@namespace, otherStorage);
    }

    /// <summary>
    /// Imports the contents of another storage into this one, using the specified namespace as a prefix for the keys.
    /// </summary>
    /// <param name="namespace">The namespace to use as a prefix for the keys.</param>
    /// <param name="other">The other storage to import from.</param>
    public void Import(string @namespace, ResourceTypeStorage<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);

        foreach (var kvp in other._storage)
        {
            var key = Identifier.Resolve(kvp.Key, @namespace);
            _storage[key] = kvp.Value;
        }
    }

    public T Get(string key)
    {
        return _storage[key];
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out T value)
    {
        return _storage.TryGetValue(key, out value);
    }

    public bool TryGetValue(int index, [MaybeNullWhen(false)] out T value)
    {
        if(index < 0 || index > _storage.Count)
        {
            value = null;
            return false;
        }

        value = _storage.Values[index];
        return true;
    }

    public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
    {
        return _storage.GetEnumerator();
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
        // ReSharper disable once GenericEnumeratorNotDisposed
        var enumerator = ((IEnumerable)storage).GetEnumerator();
        while (enumerator.MoveNext())
        {
            var kvp = enumerator.Current!;
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
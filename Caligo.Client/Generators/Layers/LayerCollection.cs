using System.Collections;

namespace Caligo.Client.Generators.Layers;

public class LayerCollection : IList<ILayer>
{
    private readonly List<ILayer> _layers = [];

    public IEnumerator<ILayer> GetEnumerator()
    {
        return _layers.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(ILayer item)
    {
        if (!ValidateLayerType(item))
            throw new InvalidOperationException("Layer of this type already exists in the collection.");

        _layers.Add(item);
    }

    public void Clear()
    {
        _layers.Clear();
    }

    public bool Contains(ILayer item)
    {
        return _layers.Contains(item);
    }

    public void CopyTo(ILayer[] array, int arrayIndex)
    {
        _layers.CopyTo(array, arrayIndex);
    }

    public bool Remove(ILayer item)
    {
        return _layers.Remove(item);
    }

    public int Count => _layers.Count;
    public bool IsReadOnly => false;

    public int IndexOf(ILayer item)
    {
        return _layers.IndexOf(item);
    }

    public void Insert(int index, ILayer item)
    {
        _layers.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        _layers.RemoveAt(index);
    }

    public ILayer this[int index]
    {
        get => _layers[index];
        set
        {
            if (!ValidateLayerType(value))
                throw new InvalidOperationException("Layer of this type already exists in the collection.");
            _layers[index] = value;
        }
    }

    private bool ValidateLayerType(ILayer item)
    {
        return !_layers.Exists(layer => layer.GetType() == item.GetType());
    }

    public T GetLayer<T>() where T : ILayer
    {
        var layer = _layers.FirstOrDefault(layer => layer is T);
        if (layer is null)
            throw new InvalidOperationException($"Layer of type {typeof(T).Name} not found in the collection.");

        return (T)layer;
    }
}
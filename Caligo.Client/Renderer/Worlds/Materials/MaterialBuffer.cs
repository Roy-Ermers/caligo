namespace Caligo.Client.Renderer.Worlds.Materials;

public class MaterialBuffer
{
    private readonly ReaderWriterLockSlim _lock = new();
    private int[] _encodedMaterials = [];
    private List<Material> Materials { get; } = [];
    public bool IsDirty { get; private set; } = true;

    public int Count => Materials.Count;
    public int EncodedLength => _encodedMaterials.Length;

    public int Add(Material material)
    {
        try
        {
            _lock.EnterWriteLock();
            var index = Materials.IndexOf(material);
            if (index >= 0)
                return index;

            IsDirty = true;

            Materials.Add(material);

            return Materials.Count - 1;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Clear()
    {
        Materials.Clear();
    }

    public int[] Encode()
    {
        if (!IsDirty)
            return _encodedMaterials;

        try
        {
            _lock.EnterReadLock();
            _encodedMaterials = [.. Materials.SelectMany(m => m.Encode())];

            IsDirty = false;
            return _encodedMaterials;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
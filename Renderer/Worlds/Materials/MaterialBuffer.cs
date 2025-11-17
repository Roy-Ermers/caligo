namespace WorldGen.Renderer.Worlds.Materials;

public class MaterialBuffer
{
    private List<Material> Materials { get; } = [];
    private bool _isDirty = true;
    public bool IsDirty => _isDirty;
    private int[] _encodedMaterials = [];

    public int Count => Materials.Count;
    public int EncodedLength => _encodedMaterials.Length;
    
    ReaderWriterLockSlim _lock = new();

    public int Add(Material material)
    {
        try {
             _lock.EnterWriteLock();
            var index = Materials.IndexOf(material);
            if (index >= 0)
                return index;

            _isDirty = true;

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
        if (!_isDirty)
            return _encodedMaterials;

        try
        {
            _lock.EnterReadLock();
            _encodedMaterials = [.. Materials.SelectMany(m => m.Encode())];

            _isDirty = false;
            return _encodedMaterials;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }


}

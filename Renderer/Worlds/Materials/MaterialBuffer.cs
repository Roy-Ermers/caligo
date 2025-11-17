namespace WorldGen.Renderer.Worlds.Materials;

public class MaterialBuffer
{
    public List<Material> Materials { get; } = [];
    private bool _isDirty = true;
    public bool IsDirty => _isDirty;
    private int[] _encodedMaterials = [];

    public int Count => Materials.Count;
    public int EncodedLength => _encodedMaterials.Length;

    public int Add(Material material)
    {
        var index = Materials.IndexOf(material);
        if (index >= 0)
            return index;

        _isDirty = true;

        Materials.Add(material);
        return Materials.Count - 1;
    }

    public void Clear()
    {
        Materials.Clear();
    }

    public int[] Encode()
    {
        if (!_isDirty)
            return _encodedMaterials;

        _encodedMaterials = [.. Materials.SelectMany(m => m.Encode())];

        _isDirty = false;
        return _encodedMaterials;
    }


}

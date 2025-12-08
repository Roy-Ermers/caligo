namespace Caligo.Core.Spatial;

public delegate float HeightmapDelegate(float x, float z);

public class Heightmap
{
    private readonly HeightmapDelegate _heightMap;


    public Heightmap(HeightmapDelegate sample, int lowestLOD = 1)
    {
        LowestLOD = lowestLOD;
        _heightMap = sample;
    }

    public int LowestLOD { get; }

    public float GetHeightAt(float x, float z)
    {
        return GetHeightAt(x, z, LowestLOD);
    }

    public float GetHeightAt(float x, float z, int LOD)
    {
        if (LOD <= 1)
            return _heightMap(x, z);

        var step = 1 << (LOD - 1);
        var x0 = x / step * step;
        var x1 = x0 + step;
        var z0 = z / step * step;
        var z1 = z0 + step;

        var h00 = _heightMap(x0, z0);
        var h10 = _heightMap(x1, z0);
        var h01 = _heightMap(x0, z1);
        var h11 = _heightMap(x1, z1);

        var tx = (x - x0) / step;
        var tz = (z - z0) / step;

        var h0 = h00 * (1 - tx) + h10 * tx;
        var h1 = h01 * (1 - tx) + h11 * tx;

        return h0 * (1 - tz) + h1 * tz;
    }
}
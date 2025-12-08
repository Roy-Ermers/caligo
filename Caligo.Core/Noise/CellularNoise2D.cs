using System.Runtime.CompilerServices;

namespace Caligo.Core.Noise;

public class CellularNoise(int seed = 0)
{
    private static readonly NoisePeriod _noPeriod = new(10, 10);
    private readonly int Seed = seed;

    /// <summary>A noise function of random polygonal cells resembling a voronoi diagram. </summary>
    public CellularResults Get(float x, float y)
    {
        var ix = x > 0 ? (int)x : (int)x - 1;
        var iy = y > 0 ? (int)y : (int)y - 1;
        var fx = x - ix;
        var fy = y - iy;

        ix += Seed * NoiseConstants.SeedPrime;

        var cx = ix * NoiseConstants.XPrime1;
        var rx = (cx + NoiseConstants.XPrime1) >> NoiseConstants.PeriodShift;
        var lx = (cx - NoiseConstants.XPrime1) >> NoiseConstants.PeriodShift;
        cx >>= NoiseConstants.PeriodShift;
        var cy = iy * NoiseConstants.YPrime1;
        var uy = (cy + NoiseConstants.YPrime1) >> NoiseConstants.PeriodShift;
        var ly = (cy - NoiseConstants.YPrime1) >> NoiseConstants.PeriodShift;
        cy >>= NoiseConstants.PeriodShift;

        return SearchNeighborhood(fx, fy,
            NoiseHelpers.Hash(lx, ly), NoiseHelpers.Hash(cx, ly), NoiseHelpers.Hash(rx, ly),
            NoiseHelpers.Hash(lx, cy), NoiseHelpers.Hash(cx, cy), NoiseHelpers.Hash(rx, cy),
            NoiseHelpers.Hash(lx, uy), NoiseHelpers.Hash(cx, uy), NoiseHelpers.Hash(rx, uy));
    }

    /// <summary>A periodic noise function of random polygonal cells resembling a voronoi diagram. </summary>
    public CellularResults GetPeriodic(float x, float y, in NoisePeriod period)
    {
        // See comments in GradientNoisePeriodic(). differences are documented.
        var ix = x > 0 ? (int)x : (int)x - 1;
        var iy = y > 0 ? (int)y : (int)y - 1;
        var fx = x - ix;
        var fy = y - iy;

        ix += Seed * NoiseConstants.SeedPrime;

        // r: right c: center l: left/lower u: upper
        // worley uses 3x3 as supposed to gradient using 2x2
        var cx = ix * period.xf;
        var rx = (cx + period.xf) >> NoiseConstants.PeriodShift;
        var lx = (cx - period.xf) >> NoiseConstants.PeriodShift;
        cx >>= NoiseConstants.PeriodShift;

        var cy = iy * period.yf;
        var uy = (cy + period.yf) >> NoiseConstants.PeriodShift;
        var ly = (cy - period.yf) >> NoiseConstants.PeriodShift;
        cy >>= NoiseConstants.PeriodShift;

        return SearchNeighborhood(fx, fy,
            NoiseHelpers.Hash(lx, ly), NoiseHelpers.Hash(cx, ly), NoiseHelpers.Hash(rx, ly),
            NoiseHelpers.Hash(lx, cy), NoiseHelpers.Hash(cx, cy), NoiseHelpers.Hash(rx, cy),
            NoiseHelpers.Hash(lx, uy), NoiseHelpers.Hash(cx, uy), NoiseHelpers.Hash(rx, uy));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe CellularResults SearchNeighborhood(float fx, float fy, int llh, int lch, int lrh, int clh,
        int cch, int crh, int ulh, int uch, int urh)
    {
        // intermediate variables
        int xHash, yHash;
        float dx, dy, sqDist, temp;
        // output variables
        var r = 0;
        float d0 = 1, d1 = 1;

        fy += 2f;

        // bottom row
        // the WorleyAndMask and WorleyOrMask set the sign bit to zero so the number is poitive and
        // set the exponent to 1 so that the value is between 1 and 2. This is why the offsets to fx / fy
        // range from 0 to 2 instead of -1 to 1.
        xHash = (llh & NoiseConstants.WorleyAndMask) | NoiseConstants.WorleyOrMask;
        yHash = xHash << 13;
        dx = fx - *(float*)&xHash + 2f;
        dy = fy - *(float*)&yHash;
        sqDist = dx * dx + dy * dy;
        r = sqDist < d0 ? llh : r;
        d1 = sqDist < d1 ? sqDist : d1; // min
        temp = d0 > d1 ? d0 : d1; // max
        d0 = d0 < d1 ? d0 : d1;
        d1 = temp;

        xHash = (lch & NoiseConstants.WorleyAndMask) | NoiseConstants.WorleyOrMask;
        yHash = xHash << 13;
        dx = fx - *(float*)&xHash + 1f;
        dy = fy - *(float*)&yHash;
        sqDist = dx * dx + dy * dy;
        r = sqDist < d0 ? lch : r;
        d1 = sqDist < d1 ? sqDist : d1; // min
        temp = d0 > d1 ? d0 : d1; // max
        d0 = d0 < d1 ? d0 : d1; // min
        d1 = temp;

        xHash = (lrh & NoiseConstants.WorleyAndMask) | NoiseConstants.WorleyOrMask;
        yHash = xHash << 13;
        dx = fx - *(float*)&xHash + 0f;
        dy = fy - *(float*)&yHash;
        sqDist = dx * dx + dy * dy;
        r = sqDist < d0 ? lrh : r;
        d1 = sqDist < d1 ? sqDist : d1; // min
        temp = d0 > d1 ? d0 : d1; // max
        d0 = d0 < d1 ? d0 : d1;
        d1 = temp;

        fy -= 1f;

        // middle row
        xHash = (clh & NoiseConstants.WorleyAndMask) | NoiseConstants.WorleyOrMask;
        yHash = xHash << 13;
        dx = fx - *(float*)&xHash + 2f;
        dy = fy - *(float*)&yHash;
        sqDist = dx * dx + dy * dy;
        r = sqDist < d0 ? clh : r;
        d1 = sqDist < d1 ? sqDist : d1; // min
        temp = d0 > d1 ? d0 : d1; // max
        d0 = d0 < d1 ? d0 : d1;
        d1 = temp;

        xHash = (cch & NoiseConstants.WorleyAndMask) | NoiseConstants.WorleyOrMask;
        yHash = xHash << 13;
        dx = fx - *(float*)&xHash + 1f;
        dy = fy - *(float*)&yHash;
        sqDist = dx * dx + dy * dy;
        r = sqDist < d0 ? cch : r;
        d1 = sqDist < d1 ? sqDist : d1; // min
        temp = d0 > d1 ? d0 : d1; // max
        d0 = d0 < d1 ? d0 : d1; // min
        d1 = temp;

        xHash = (crh & NoiseConstants.WorleyAndMask) | NoiseConstants.WorleyOrMask;
        yHash = xHash << 13;
        dx = fx - *(float*)&xHash + 0f;
        dy = fy - *(float*)&yHash;
        sqDist = dx * dx + dy * dy;
        r = sqDist < d0 ? crh : r;
        d1 = sqDist < d1 ? sqDist : d1; // min
        temp = d0 > d1 ? d0 : d1; // max
        d0 = d0 < d1 ? d0 : d1;
        d1 = temp;

        fy -= 1f;

        // top row
        xHash = (ulh & NoiseConstants.WorleyAndMask) | NoiseConstants.WorleyOrMask;
        yHash = xHash << 13;
        dx = fx - *(float*)&xHash + 2f;
        dy = fy - *(float*)&yHash;
        sqDist = dx * dx + dy * dy;
        r = sqDist < d0 ? ulh : r;
        d1 = sqDist < d1 ? sqDist : d1; // min
        temp = d0 > d1 ? d0 : d1; // max
        d0 = d0 < d1 ? d0 : d1;
        d1 = temp;

        xHash = (uch & NoiseConstants.WorleyAndMask) | NoiseConstants.WorleyOrMask;
        yHash = xHash << 13;
        dx = fx - *(float*)&xHash + 1f;
        dy = fy - *(float*)&yHash;
        sqDist = dx * dx + dy * dy;
        r = sqDist < d0 ? uch : r;
        d1 = sqDist < d1 ? sqDist : d1; // min
        temp = d0 > d1 ? d0 : d1; // max
        d0 = d0 < d1 ? d0 : d1; // min
        d1 = temp;

        xHash = (urh & NoiseConstants.WorleyAndMask) | NoiseConstants.WorleyOrMask;
        yHash = xHash << 13;
        dx = fx - *(float*)&xHash + 0f;
        dy = fy - *(float*)&yHash;
        sqDist = dx * dx + dy * dy;
        r = sqDist < d0 ? urh : r;
        d1 = sqDist < d1 ? sqDist : d1; // min
        temp = d0 > d1 ? d0 : d1; // max
        d0 = d0 < d1 ? d0 : d1;

        d1 = temp;

        d0 = MathF.Sqrt(d0);
        d1 = MathF.Sqrt(d1);

        r = ((r * NoiseConstants.ZPrime1) & NoiseConstants.PortionAndMask) | NoiseConstants.PortionOrMask;
        var rFloat = *(float*)&r - 1f;
        return new CellularResults(d0, d1, rFloat);
    }
}

/// <summary>The results of a Cellular Noise evaluation. </summary>
public readonly record struct CellularResults(
    /// <summary> The distance to the closest cell center.</summary>
    float Distance0,
    /// <summary> The distance to the second-closest cell center.</summary>
    float Distance1,
    /// <summary> A random 0 - 1 value for each cell. </summary>
    float random);
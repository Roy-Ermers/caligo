using System.Runtime.CompilerServices;

namespace Caligo.Core.Noise;

public class GradientNoise(int seed = 0)
{
    public readonly int Seed = seed;

    /// <summary> -1 to 1 gradient noise function. Analagous to Perlin noise. </summary>
    public float Get2D(float x, float y)
    {
        // NOTE: if you are looking to understand how this function works, first make sure
        // you understand the concepts behind Perlin Noise. these comments only detail
        // the specifics of this implementation.

        // break up sample coords into a float and int component,
        // (ix, iy) represent the lower-left corner of the unit square the sample is in,
        // (fx, fy) represent the 0.0 to 1.0 position within that square
        // ix = floor(x) and fx = x - ix
        // iy = floor(y) and iy = y - iy
        var ix = x > 0 ? (int)x : (int)x - 1;
        var iy = y > 0 ? (int)y : (int)y - 1;
        var fx = x - ix;
        var fy = y - iy;

        // Hashes for non-periodic noise are the product of two linear fields p1 and p2, where
        // p1 = x * XPrime1 + y * YPrime1 (XPrime1 and YPrime1 are constant 32-bit primes)
        // p2 = x * XPrime2 + y * YPrime2 (XPrime2 and YPrime2 are constant 32-bit primes)
        // adding a constant to the value of these fields at the lower-left corner of the square can get
        // you the value at the remaining 3 corners, which reduces the multiplies per hash by a factor of 3.
        // this behaves poorly at x = 0 or y = 0 so we add a very large constant offset
        // to the x and y coordinates before calculating the hash.
        ix += NoiseConstants.Offset;
        iy += NoiseConstants.Offset;
        ix += NoiseConstants.SeedPrime * Seed; // add seed before hashing to propigate its effect
        var p1 = ix * NoiseConstants.XPrime1 + iy * NoiseConstants.YPrime1;
        var p2 = ix * NoiseConstants.XPrime2 + iy * NoiseConstants.YPrime2;
        var llHash = p1 * p2;
        var lrHash = (p1 + NoiseConstants.XPrime1) * (p2 + NoiseConstants.XPrime2);
        var ulHash = (p1 + NoiseConstants.YPrime1) * (p2 + NoiseConstants.YPrime2);
        var urHash = (p1 + NoiseConstants.XPlusYPrime1) * (p2 + NoiseConstants.XPlusYPrime2);
        return InterpolateGradients2D(llHash, lrHash, ulHash, urHash, fx, fy);
    }

    /// <summary>Two seperatly seeded fields of -1 to 1 gradient noise. Analagous to Perlin noise.</summary>
    public (float X, float Y) Get2DVector(float x, float y)
    {
        var ix = x > 0 ? (int)x : (int)x - 1;
        var iy = y > 0 ? (int)y : (int)y - 1;
        var fx = x - ix;
        var fy = y - iy;

        ix += NoiseConstants.Offset;
        iy += NoiseConstants.Offset;
        ix += NoiseConstants.SeedPrime * Seed; // add seed before hashing to propigate its effect
        var p1 = ix * NoiseConstants.XPrime1 + iy * NoiseConstants.YPrime1;
        var p2 = ix * NoiseConstants.XPrime2 + iy * NoiseConstants.YPrime2;
        var llHash = p1 * p2;
        var lrHash = (p1 + NoiseConstants.XPrime1) * (p2 + NoiseConstants.XPrime2);
        var ulHash = (p1 + NoiseConstants.YPrime1) * (p2 + NoiseConstants.YPrime2);
        var urHash = (p1 + NoiseConstants.XPlusYPrime1) * (p2 + NoiseConstants.XPlusYPrime2);

        x = InterpolateGradients2D(llHash, lrHash, ulHash, urHash, fx, fy);
        // multiplying by a 32-bit value is all you need to reseed already randomized bits.
        y = InterpolateGradients2D(
            llHash * NoiseConstants.XPrime1, lrHash * NoiseConstants.XPrime1,
            ulHash * NoiseConstants.XPrime1, urHash * NoiseConstants.XPrime1, fx, fy);
        return (x, y);
    }

    /// <summary>Periodic variant of -1 to 1 gradient noise function. Analagous to Perlin Noise.</summary>
    public float Get1DPeriodic(float x, float y, in NoisePeriod period)
    {
        // see comments in GradientNoise()
        var ix = x > 0 ? (int)x : (int)x - 1;
        var iy = y > 0 ? (int)y : (int)y - 1;
        var fx = x - ix;
        var fy = y - iy;
        var seed = Seed;

        seed *= NoiseConstants.SeedPrime << NoiseConstants.PeriodShift;
        ix += seed;
        iy += seed;

        // the trick used for hashing on non-periodic noise doesn't work here.
        // instead we create a periodic value for each coordinate using a multiply and bitshift
        // instead of a mod operator, then plug those values into an efficient hash function.
        // left, lower, right, and upper are the periodic hash inputs.
        // period.xf = uint.MaxValue / xPeriod and
        // period.yf = uint.MaxValue / yPeriod.
        // this means that the multiply wraps back to zero at the period with an overflow
        // that doesn't effect the bits and a slight error that is removed by a right shift.
        var left = ix * period.xf;
        var lower = iy * period.yf;
        var right = left + period.xf;
        var upper = lower + period.yf;
        left >>= NoiseConstants.PeriodShift;
        lower >>= NoiseConstants.PeriodShift;
        right >>= NoiseConstants.PeriodShift;
        upper >>= NoiseConstants.PeriodShift;
        var llHash = NoiseHelpers.Hash(left, lower);
        var lrHash = NoiseHelpers.Hash(right, lower);
        var ulHash = NoiseHelpers.Hash(left, upper);
        var urHash = NoiseHelpers.Hash(right, upper);
        return InterpolateGradients2D(llHash, lrHash, ulHash, urHash, fx, fy);
    }


    /// <summary>
    ///     Two seperately seeded periodic -1 to 1 gradient noise functions.
    ///     Analagous to Perlin Noise.
    /// </summary>
    public (float x, float y) GradientNoisePeriodicVec2(float x, float y, in NoisePeriod period)
    {
        // see comments in GradientNoisePeriodic() and GradientNoise()
        var ix = x > 0 ? (int)x : (int)x - 1;
        var iy = y > 0 ? (int)y : (int)y - 1;
        var fx = x - ix;
        var fy = y - iy;

        var seed = Seed;

        seed *= NoiseConstants.SeedPrime << NoiseConstants.PeriodShift;
        ix += seed;
        iy += seed;

        var left = ix * period.xf; // left
        var lower = iy * period.yf; // lower
        var right = left + period.xf; // right
        var upper = lower + period.yf; // upper
        left >>= NoiseConstants.PeriodShift;
        lower >>= NoiseConstants.PeriodShift;
        right >>= NoiseConstants.PeriodShift;
        upper >>= NoiseConstants.PeriodShift;

        var llHash = NoiseHelpers.Hash(left, lower);
        var lrHash = NoiseHelpers.Hash(right, lower);
        var ulHash = NoiseHelpers.Hash(left, upper);
        var urHash = NoiseHelpers.Hash(right, upper);
        x = InterpolateGradients2D(llHash, lrHash, ulHash, urHash, fx, fy);
        y = InterpolateGradients2D(
            llHash * NoiseConstants.XPrime1, lrHash * NoiseConstants.XPrime1,
            ulHash * NoiseConstants.XPrime1, urHash * NoiseConstants.XPrime1, fx, fy);
        return (x, y);
    }

    /// <summary>
    ///     High-quality version of GradientNoise() that returns a rotated
    ///     slice of 3D gradient noise to remove grid alignment artifacts.
    /// </summary>
    [MethodImpl(512)] // aggressive optimization on supported runtimes
    public float GetHighQuality1D(float x, float y)
    {
        // rotation from https://noiseposti.ng/posts/2022-01-16-The-Perlin-Problem-Breaking-The-Cycle.html
        var xy = x + y;
        var s2 = xy * -0.2113248f;
        var z = xy * -0.5773502f;
        x += s2;
        y += s2;

        // GradientNoise3D() won't get inlined automatically so its manually inlined here.
        // seems to improve preformance by around 5 to 10%
        var ix = x > 0 ? (int)x : (int)x - 1;
        var iy = y > 0 ? (int)y : (int)y - 1;
        var iz = z > 0 ? (int)z : (int)z - 1;
        var fx = x - ix;
        var fy = y - iy;
        var fz = z - iz;

        ix += Seed * NoiseConstants.SeedPrime;

        ix += NoiseConstants.Offset;
        iy += NoiseConstants.Offset;
        iz += NoiseConstants.Offset;
        var p1 = ix * NoiseConstants.XPrime1 + iy * NoiseConstants.YPrime1 + iz * NoiseConstants.ZPrime1;
        var p2 = ix * NoiseConstants.XPrime2 + iy * NoiseConstants.YPrime2 + iz * NoiseConstants.ZPrime2;
        var llHash = p1 * p2;
        var lrHash = (p1 + NoiseConstants.XPrime1) * (p2 + NoiseConstants.XPrime2);
        var ulHash = (p1 + NoiseConstants.YPrime1) * (p2 + NoiseConstants.YPrime2);
        var urHash = (p1 + NoiseConstants.XPlusYPrime1) * (p2 + NoiseConstants.XPlusYPrime2);
        var zLowBlend = InterpolateGradients3D(llHash, lrHash, ulHash, urHash, fx, fy, fz);
        llHash = (p1 + NoiseConstants.ZPrime1) * (p2 + NoiseConstants.ZPrime2);
        lrHash = (p1 + NoiseConstants.XPlusZPrime1) * (p2 + NoiseConstants.XPlusZPrime2);
        ulHash = (p1 + NoiseConstants.YPlusZPrime1) * (p2 + NoiseConstants.YPlusZPrime2);
        urHash = (p1 + NoiseConstants.XPlusYPlusZPrime1) * (p2 + NoiseConstants.XPlusYPlusZPrime2);
        var zHighBlend = InterpolateGradients3D(llHash, lrHash, ulHash, urHash, fx, fy, fz - 1);
        var sz = fz * fz * (3 - 2 * fz);
        return zLowBlend + (zHighBlend - zLowBlend) * sz;
    }

    /// <summary> 3D -1 to 1 gradient noise function. Analagous to Perlin Noise. </summary>
    [MethodImpl(512)] // aggressive optimization on supported runtimes
    public float Get3D(float x, float y, float z)
    {
        // see comments in GradientNoise()
        var ix = x > 0 ? (int)x : (int)x - 1;
        var iy = y > 0 ? (int)y : (int)y - 1;
        var iz = z > 0 ? (int)z : (int)z - 1;
        var fx = x - ix;
        var fy = y - iy;
        var fz = z - iz;

        ix += Seed * NoiseConstants.SeedPrime;

        ix += NoiseConstants.Offset;
        iy += NoiseConstants.Offset;
        iz += NoiseConstants.Offset;
        var p1 = ix * NoiseConstants.XPrime1 + iy * NoiseConstants.YPrime1 + iz * NoiseConstants.ZPrime1;
        var p2 = ix * NoiseConstants.XPrime2 + iy * NoiseConstants.YPrime2 + iz * NoiseConstants.ZPrime2;
        var llHash = p1 * p2;
        var lrHash = (p1 + NoiseConstants.XPrime1) * (p2 + NoiseConstants.XPrime2);
        var ulHash = (p1 + NoiseConstants.YPrime1) * (p2 + NoiseConstants.YPrime2);
        var urHash = (p1 + NoiseConstants.XPlusYPrime1) * (p2 + NoiseConstants.XPlusYPrime2);
        var zLowBlend = InterpolateGradients3D(llHash, lrHash, ulHash, urHash, fx, fy, fz);
        llHash = (p1 + NoiseConstants.ZPrime1) * (p2 + NoiseConstants.ZPrime2);
        lrHash = (p1 + NoiseConstants.XPlusZPrime1) * (p2 + NoiseConstants.XPlusZPrime2);
        ulHash = (p1 + NoiseConstants.YPlusZPrime1) * (p2 + NoiseConstants.YPlusZPrime2);
        urHash = (p1 + NoiseConstants.XPlusYPlusZPrime1) * (p2 + NoiseConstants.XPlusYPlusZPrime2);
        var zHighBlend = InterpolateGradients3D(llHash, lrHash, ulHash, urHash, fx, fy, fz - 1);
        var sz = fz * fz * (3 - 2 * fz);
        return zLowBlend + (zHighBlend - zLowBlend) * sz;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe float InterpolateGradients3D(int llHash, int lrHash, int ulHash, int urHash, float fx,
        float fy, float fz)
    {
        // see comments in InterpolateGradients2D()
        int xHash, yHash, zHash;
        xHash = (llHash & NoiseConstants.GradAndMask) | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        zHash = xHash << NoiseConstants.GradShift2;
        var llGrad = fx * *(float*)&xHash + fy * *(float*)&yHash + fz * *(float*)&zHash; // dot-product
        xHash = (lrHash & NoiseConstants.GradAndMask) | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        zHash = xHash << NoiseConstants.GradShift2;
        var lrGrad = (fx - 1) * *(float*)&xHash + fy * *(float*)&yHash + fz * *(float*)&zHash;
        xHash = (ulHash & NoiseConstants.GradAndMask) | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        zHash = xHash << NoiseConstants.GradShift2;
        var ulGrad = fx * *(float*)&xHash + (fy - 1) * *(float*)&yHash + fz * *(float*)&zHash; // dot-product
        xHash = (urHash & NoiseConstants.GradAndMask) | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        zHash = xHash << NoiseConstants.GradShift2;
        var urGrad = (fx - 1) * *(float*)&xHash + (fy - 1) * *(float*)&yHash + fz * *(float*)&zHash;
        var sx = fx * fx * (3 - 2 * fx);
        var sy = fy * fy * (3 - 2 * fy);
        var lowerBlend = llGrad + (lrGrad - llGrad) * sx;
        var upperBlend = ulGrad + (urGrad - ulGrad) * sx;
        return lowerBlend + (upperBlend - lowerBlend) * sy;
    }

    /// <summary>Evaluates and interpolates the gradients at each corner.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe float InterpolateGradients2D(int llHash, int lrHash, int ulHash, int urHash, float fx,
        float fy)
    {
        // here we calculate a gradient at each corner, where the value is the dot-product
        // of a vector derived from the hash and the vector from the coner to the
        // sample point. these vectors are blended using bilinear interpolation
        // and the result is the return value for the noise function.
        // to convert a hash value to a vector, we reinterpret the random bits
        // as a floating-point number, but use bitmasks to set the exponent
        // of the value to 0.5, which makes the range of output results is
        // -1 to -0.5 and 0.5 to 1. With this value in both channels our vector
        // can face along any diagonal axis and has a magnitude close to 1,
        // which is a good enough distribution of vectors for gradient noise.
        // to avoid having to calculate the mask twice for the x and y coordinates,
        // the mask has a second copy of the exponent bits in unsignifigant bits
        // of the mantissa, so bitshifting the masked hash to align the second exponent
        // gives a second random float in the same range as the first.
        // this could be broken up into functions but doing so massively hurts
        // preformance without optimizations enabled.
        int xHash, yHash;
        xHash = (llHash & NoiseConstants.GradAndMask) | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        var llGrad = fx * *(float*)&xHash + fy * *(float*)&yHash; // dot-product
        xHash = (lrHash & NoiseConstants.GradAndMask) | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        var lrGrad = (fx - 1) * *(float*)&xHash + fy * *(float*)&yHash;
        xHash = (ulHash & NoiseConstants.GradAndMask) | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        var ulGrad = fx * *(float*)&xHash + (fy - 1) * *(float*)&yHash; // dot-product
        xHash = (urHash & NoiseConstants.GradAndMask) | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        var urGrad = (fx - 1) * *(float*)&xHash + (fy - 1) * *(float*)&yHash;
        // adjust blending values with the smoothstep function s(x) = x * x * (3 - 2 * x)
        // which gives a result close to x but with a slope of zero at x = 0 and x = 1.
        // this makes the blending transitions between cells less harsh.
        var sx = fx * fx * (3 - 2 * fx);
        var sy = fy * fy * (3 - 2 * fy);
        var lowerBlend = llGrad + (lrGrad - llGrad) * sx;
        var upperBlend = ulGrad + (urGrad - ulGrad) * sx;
        return lowerBlend + (upperBlend - lowerBlend) * sy;
    }

    /// <summary>Calculates a 2D gradient based on a hash value and coordinates relative to the gradient's origin.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe float EvalGradient(int hash, float fx, float fy)
    {
        // to convert a hash value to a vector, we reinterpret the random bits
        // as a floating-point number, but use bitmasks to set the exponent
        // of the value to 0.5, which makes the range of output results is
        // -1 to -0.5 and 0.5 to 1. With this value in both channels our vector
        // can face along any diagonal axis and has a magnitude close to 1,
        // which is a good enough distribution of vectors for gradient noise.
        // to avoid having to calculate the mask twice for the x and y coordinates,
        // the mask has a second copy of the exponent bits in unsignifigant bits
        // of the mantissa, so bitshifting the masked hash to align the second exponent
        // gives a second random float in the same range as the first.
        var xHash = (hash & NoiseConstants.GradAndMask) | NoiseConstants.GradOrMask;
        var yHash = xHash << NoiseConstants.GradShift1;
        return fx * *(float*)&xHash + fy * *(float*)&yHash; // dot-product
    }
}
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
        int ix = x > 0 ? (int)x : (int)x - 1;
        int iy = y > 0 ? (int)y : (int)y - 1;
        float fx = x - ix;
        float fy = y - iy;

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
        int p1 = ix * NoiseConstants.XPrime1 + iy * NoiseConstants.YPrime1;
        int p2 = ix * NoiseConstants.XPrime2 + iy * NoiseConstants.YPrime2;
        int llHash = p1 * p2;
        int lrHash = (p1 + NoiseConstants.XPrime1) * (p2 + NoiseConstants.XPrime2);
        int ulHash = (p1 + NoiseConstants.YPrime1) * (p2 + NoiseConstants.YPrime2);
        int urHash = (p1 + NoiseConstants.XPlusYPrime1) * (p2 + NoiseConstants.XPlusYPrime2);
        return InterpolateGradients2D(llHash, lrHash, ulHash, urHash, fx, fy);
    }

    /// <summary>Two seperatly seeded fields of -1 to 1 gradient noise. Analagous to Perlin noise.</summary>
    public (float X, float Y) Get2DVector(float x, float y)
    {
        int ix = x > 0 ? (int)x : (int)x - 1;
        int iy = y > 0 ? (int)y : (int)y - 1;
        float fx = x - ix;
        float fy = y - iy;

        ix += NoiseConstants.Offset;
        iy += NoiseConstants.Offset;
        ix += NoiseConstants.SeedPrime * Seed; // add seed before hashing to propigate its effect
        int p1 = ix * NoiseConstants.XPrime1 + iy * NoiseConstants.YPrime1;
        int p2 = ix * NoiseConstants.XPrime2 + iy * NoiseConstants.YPrime2;
        int llHash = p1 * p2;
        int lrHash = (p1 + NoiseConstants.XPrime1) * (p2 + NoiseConstants.XPrime2);
        int ulHash = (p1 + NoiseConstants.YPrime1) * (p2 + NoiseConstants.YPrime2);
        int urHash = (p1 + NoiseConstants.XPlusYPrime1) * (p2 + NoiseConstants.XPlusYPrime2);

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
        int ix = x > 0 ? (int)x : (int)x - 1;
        int iy = y > 0 ? (int)y : (int)y - 1;
        float fx = x - ix;
        float fy = y - iy;
        int seed = Seed;

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
        int left = ix * period.xf;
        int lower = iy * period.yf;
        int right = left + period.xf;
        int upper = lower + period.yf;
        left >>= NoiseConstants.PeriodShift;
        lower >>= NoiseConstants.PeriodShift;
        right >>= NoiseConstants.PeriodShift;
        upper >>= NoiseConstants.PeriodShift;
        int llHash = NoiseHelpers.Hash(left, lower);
        int lrHash = NoiseHelpers.Hash(right, lower);
        int ulHash = NoiseHelpers.Hash(left, upper);
        int urHash = NoiseHelpers.Hash(right, upper);
        return InterpolateGradients2D(llHash, lrHash, ulHash, urHash, fx, fy);
    }


    /// <summary>Two seperately seeded periodic -1 to 1 gradient noise functions.
    /// Analagous to Perlin Noise.</summary>
    public (float x, float y) GradientNoisePeriodicVec2(float x, float y, in NoisePeriod period)
    {
        // see comments in GradientNoisePeriodic() and GradientNoise()
        int ix = x > 0 ? (int)x : (int)x - 1;
        int iy = y > 0 ? (int)y : (int)y - 1;
        float fx = x - ix;
        float fy = y - iy;

        int seed = Seed;

        seed *= NoiseConstants.SeedPrime << NoiseConstants.PeriodShift;
        ix += seed;
        iy += seed;

        int left = ix * period.xf; // left
        int lower = iy * period.yf; // lower
        int right = left + period.xf; // right
        int upper = lower + period.yf; // upper
        left >>= NoiseConstants.PeriodShift;
        lower >>= NoiseConstants.PeriodShift;
        right >>= NoiseConstants.PeriodShift;
        upper >>= NoiseConstants.PeriodShift;

        int llHash = NoiseHelpers.Hash(left, lower);
        int lrHash = NoiseHelpers.Hash(right, lower);
        int ulHash = NoiseHelpers.Hash(left, upper);
        int urHash = NoiseHelpers.Hash(right, upper);
        x = InterpolateGradients2D(llHash, lrHash, ulHash, urHash, fx, fy);
        y = InterpolateGradients2D(
            llHash * NoiseConstants.XPrime1, lrHash * NoiseConstants.XPrime1,
            ulHash * NoiseConstants.XPrime1, urHash * NoiseConstants.XPrime1, fx, fy);
        return (x, y);
    }

    /// <summary>High-quality version of GradientNoise() that returns a rotated
    /// slice of 3D gradient noise to remove grid alignment artifacts.</summary>
    [MethodImpl(512)] // aggressive optimization on supported runtimes
    public float GetHighQuality1D(float x, float y)
    {
        // rotation from https://noiseposti.ng/posts/2022-01-16-The-Perlin-Problem-Breaking-The-Cycle.html
        float xy = x + y;
        float s2 = xy * -0.2113248f;
        float z = xy * -0.5773502f;
        x += s2;
        y += s2;

        // GradientNoise3D() won't get inlined automatically so its manually inlined here.
        // seems to improve preformance by around 5 to 10%
        int ix = x > 0 ? (int)x : (int)x - 1;
        int iy = y > 0 ? (int)y : (int)y - 1;
        int iz = z > 0 ? (int)z : (int)z - 1;
        float fx = x - ix;
        float fy = y - iy;
        float fz = z - iz;

        ix += Seed * NoiseConstants.SeedPrime;

        ix += NoiseConstants.Offset;
        iy += NoiseConstants.Offset;
        iz += NoiseConstants.Offset;
        int p1 = ix * NoiseConstants.XPrime1 + iy * NoiseConstants.YPrime1 + iz * NoiseConstants.ZPrime1;
        int p2 = ix * NoiseConstants.XPrime2 + iy * NoiseConstants.YPrime2 + iz * NoiseConstants.ZPrime2;
        int llHash = p1 * p2;
        int lrHash = (p1 + NoiseConstants.XPrime1) * (p2 + NoiseConstants.XPrime2);
        int ulHash = (p1 + NoiseConstants.YPrime1) * (p2 + NoiseConstants.YPrime2);
        int urHash = (p1 + NoiseConstants.XPlusYPrime1) * (p2 + NoiseConstants.XPlusYPrime2);
        float zLowBlend = InterpolateGradients3D(llHash, lrHash, ulHash, urHash, fx, fy, fz);
        llHash = (p1 + NoiseConstants.ZPrime1) * (p2 + NoiseConstants.ZPrime2);
        lrHash = (p1 + NoiseConstants.XPlusZPrime1) * (p2 + NoiseConstants.XPlusZPrime2);
        ulHash = (p1 + NoiseConstants.YPlusZPrime1) * (p2 + NoiseConstants.YPlusZPrime2);
        urHash = (p1 + NoiseConstants.XPlusYPlusZPrime1) * (p2 + NoiseConstants.XPlusYPlusZPrime2);
        float zHighBlend = InterpolateGradients3D(llHash, lrHash, ulHash, urHash, fx, fy, fz - 1);
        float sz = fz * fz * (3 - 2 * fz);
        return zLowBlend + (zHighBlend - zLowBlend) * sz;
    }

    /// <summary> 3D -1 to 1 gradient noise function. Analagous to Perlin Noise. </summary>
    [MethodImpl(512)] // aggressive optimization on supported runtimes
    public float Get3D(float x, float y, float z)
    {
        // see comments in GradientNoise()
        int ix = x > 0 ? (int)x : (int)x - 1;
        int iy = y > 0 ? (int)y : (int)y - 1;
        int iz = z > 0 ? (int)z : (int)z - 1;
        float fx = x - ix;
        float fy = y - iy;
        float fz = z - iz;

        ix += Seed * NoiseConstants.SeedPrime;

        ix += NoiseConstants.Offset;
        iy += NoiseConstants.Offset;
        iz += NoiseConstants.Offset;
        int p1 = ix * NoiseConstants.XPrime1 + iy * NoiseConstants.YPrime1 + iz * NoiseConstants.ZPrime1;
        int p2 = ix * NoiseConstants.XPrime2 + iy * NoiseConstants.YPrime2 + iz * NoiseConstants.ZPrime2;
        int llHash = p1 * p2;
        int lrHash = (p1 + NoiseConstants.XPrime1) * (p2 + NoiseConstants.XPrime2);
        int ulHash = (p1 + NoiseConstants.YPrime1) * (p2 + NoiseConstants.YPrime2);
        int urHash = (p1 + NoiseConstants.XPlusYPrime1) * (p2 + NoiseConstants.XPlusYPrime2);
        float zLowBlend = InterpolateGradients3D(llHash, lrHash, ulHash, urHash, fx, fy, fz);
        llHash = (p1 + NoiseConstants.ZPrime1) * (p2 + NoiseConstants.ZPrime2);
        lrHash = (p1 + NoiseConstants.XPlusZPrime1) * (p2 + NoiseConstants.XPlusZPrime2);
        ulHash = (p1 + NoiseConstants.YPlusZPrime1) * (p2 + NoiseConstants.YPlusZPrime2);
        urHash = (p1 + NoiseConstants.XPlusYPlusZPrime1) * (p2 + NoiseConstants.XPlusYPlusZPrime2);
        float zHighBlend = InterpolateGradients3D(llHash, lrHash, ulHash, urHash, fx, fy, fz - 1);
        float sz = fz * fz * (3 - 2 * fz);
        return zLowBlend + (zHighBlend - zLowBlend) * sz;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static unsafe float InterpolateGradients3D(int llHash, int lrHash, int ulHash, int urHash, float fx, float fy, float fz)
    {
        // see comments in InterpolateGradients2D()
        int xHash, yHash, zHash;
        xHash = llHash & NoiseConstants.GradAndMask | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        zHash = xHash << NoiseConstants.GradShift2;
        float llGrad = fx * *(float*)&xHash + fy * *(float*)&yHash + fz * *(float*)&zHash; // dot-product
        xHash = lrHash & NoiseConstants.GradAndMask | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        zHash = xHash << NoiseConstants.GradShift2;
        float lrGrad = (fx - 1) * *(float*)&xHash + fy * *(float*)&yHash + fz * *(float*)&zHash;
        xHash = ulHash & NoiseConstants.GradAndMask | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        zHash = xHash << NoiseConstants.GradShift2;
        float ulGrad = fx * *(float*)&xHash + (fy - 1) * *(float*)&yHash + fz * *(float*)&zHash; // dot-product
        xHash = urHash & NoiseConstants.GradAndMask | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        zHash = xHash << NoiseConstants.GradShift2;
        float urGrad = (fx - 1) * *(float*)&xHash + (fy - 1) * *(float*)&yHash + fz * *(float*)&zHash;
        float sx = fx * fx * (3 - 2 * fx);
        float sy = fy * fy * (3 - 2 * fy);
        float lowerBlend = llGrad + (lrGrad - llGrad) * sx;
        float upperBlend = ulGrad + (urGrad - ulGrad) * sx;
        return lowerBlend + (upperBlend - lowerBlend) * sy;
    }

    /// <summary>Evaluates and interpolates the gradients at each corner.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static unsafe float InterpolateGradients2D(int llHash, int lrHash, int ulHash, int urHash, float fx, float fy)
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
        xHash = llHash & NoiseConstants.GradAndMask | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        float llGrad = fx * *(float*)&xHash + fy * *(float*)&yHash; // dot-product
        xHash = lrHash & NoiseConstants.GradAndMask | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        float lrGrad = (fx - 1) * *(float*)&xHash + fy * *(float*)&yHash;
        xHash = ulHash & NoiseConstants.GradAndMask | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        float ulGrad = fx * *(float*)&xHash + (fy - 1) * *(float*)&yHash; // dot-product
        xHash = urHash & NoiseConstants.GradAndMask | NoiseConstants.GradOrMask;
        yHash = xHash << NoiseConstants.GradShift1;
        float urGrad = (fx - 1) * *(float*)&xHash + (fy - 1) * *(float*)&yHash;
        // adjust blending values with the smoothstep function s(x) = x * x * (3 - 2 * x)
        // which gives a result close to x but with a slope of zero at x = 0 and x = 1.
        // this makes the blending transitions between cells less harsh.
        float sx = fx * fx * (3 - 2 * fx);
        float sy = fy * fy * (3 - 2 * fy);
        float lowerBlend = llGrad + (lrGrad - llGrad) * sx;
        float upperBlend = ulGrad + (urGrad - ulGrad) * sx;
        return lowerBlend + (upperBlend - lowerBlend) * sy;
    }

    /// <summary>Calculates a 2D gradient based on a hash value and coordinates relative to the gradient's origin.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static unsafe float EvalGradient(int hash, float fx, float fy)
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
        int xHash = hash & NoiseConstants.GradAndMask | NoiseConstants.GradOrMask;
        int yHash = xHash << NoiseConstants.GradShift1;
        return fx * *(float*)&xHash + fy * *(float*)&yHash; // dot-product
    }
}
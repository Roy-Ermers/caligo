using System.Runtime.CompilerServices;

namespace Caligo.Core.Noise;

public class NoiseHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Hash(int x, int y)
    {
        // bitshift on y to make sure NumericHelpers.Hash(x + 1, y) and NumericHelpers.Hash(x, y + 1)
        // are radically different, shifts below 6 produce visable artifacts.
        int hash = x ^ (y << 6);
        // bits passed into this hash function are in the upper part of the lower bits of an int,
        // we bit shift them slightly lower here to maximize the impact of the following multiply.
        // the lowest bit will effect all bits when multiplied, but higher bits don't effect anything
        // below them, so you want your signifigant bits as low as possible. the bitshift isn't larger
        // because then it would in some cases bitshift some of your bits off the bottom of the int,
        // which is a disaster for hash quality.
        hash += hash >> 5;
        // multiply propigates lower bits to every single bit
        hash *= NoiseConstants.XPrime1;
        // xor and add operators are nonlinear relative to eachother, so interleaving like this
        // produces the nonlinearities the hash function needs to avoid visual artifacts.
        // we are bitshifting down to make these nonlinearities occur in low bits so after the final multiply
        // they effect the largest fraction of the output hash.
        hash ^= hash >> 4;
        hash += hash >> 2;
        hash ^= hash >> 16;
        // multiply propigates lower bits to every single bit (again)
        hash *= NoiseConstants.XPrime2;
        return hash;
    }
}

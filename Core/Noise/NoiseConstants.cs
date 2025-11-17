namespace Caligo.Core.Noise;

internal static class NoiseConstants
{
    public const int FractalOctaves = 8;

    internal const int
        Offset = 0228125273,
        SeedPrime = 525124619,
        SeedMask = 0x0FFFFFFF,
        XPrime1 = 0863909317,
        YPrime1 = 1987438051,
        ZPrime1 = 1774326877,
        XPlusYPrime1 = unchecked(XPrime1 + YPrime1),
        XPlusZPrime1 = unchecked(XPrime1 + ZPrime1),
        YPlusZPrime1 = unchecked(YPrime1 + ZPrime1),
        XPlusYPlusZPrime1 = unchecked(XPrime1 + YPrime1 + ZPrime1),
        XMinusYPrime1 = unchecked(XPrime1 - YPrime1),
        YMinusXPrime1 = unchecked(XPrime1 - YPrime1),
        XPrime2 = 1299341299,
        YPrime2 = 0580423463,
        ZPrime2 = 0869819479,
        XPlusYPrime2 = unchecked(XPrime2 + YPrime2),
        XPlusZPrime2 = unchecked(XPrime2 + ZPrime2),
        YPlusZPrime2 = unchecked(YPrime2 + ZPrime2),
        XPlusYPlusZPrime2 = unchecked(XPrime2 + YPrime2 + ZPrime2),
        XMinusYPrime2 = unchecked(XPrime2 - YPrime2),
        YMinusXPrime2 = unchecked(XPrime2 - YPrime2),
        GradAndMask = -0x7F9FE7F9, //-0x7F87F801
        GradOrMask = 0x3F0FC3F0, //0x3F03F000
        GradShift1 = 10,
        GradShift2 = 20,
        PeriodShift = 18,
        WorleyAndMask = 0x007803FF,
        WorleyOrMask = 0x3F81FC00,
        PortionAndMask = 0x007FFFFF,
        PortionOrMask = 0x3F800000;
}
// ReSharper disable MemberCanBePrivate.Global

namespace Caligo.Core.Utils;

public static class Easings
{
    /// <summary>
    ///     A linear interpolation.
    ///     <code>
    ///   /
    ///  /
    /// /
    /// </code>
    /// </summary>
    public static float Linear(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    /// <summary>
    ///     An ease-in sine curve. Slow start, fast end.
    ///     <code>
    ///      /
    ///     /
    ///    /
    /// __/
    /// </code>
    /// </summary>
    public static float EaseInSine(float t)
    {
        return 1 - MathF.Cos((t * MathF.PI) / 2);
    }

    /// <summary>
    ///     An ease-out sine curve. Fast start, slow end.
    ///     <code>
    /// __
    ///   \
    ///    \
    ///     \
    ///      \
    /// </code>
    /// </summary>
    public static float EaseOutSine(float t)
    {
        return MathF.Sin((t * MathF.PI) / 2);
    }

    /// <summary>
    ///     An ease-in-out sine curve. Slow start and end.
    ///     <code>
    ///    __
    ///   /  \
    ///  /    \
    /// /      \
    /// </code>
    /// </summary>
    public static float EaseInOutSine(float t)
    {
        return -(MathF.Cos(MathF.PI * t) - 1) / 2;
    }

    /// <summary>
    ///     An ease-in quadratic curve.
    ///     <code>
    ///      /
    ///     /
    ///    /
    /// ___/
    /// </code>
    /// </summary>
    public static float EaseInQuad(float t)
    {
        return t * t;
    }

    /// <summary>
    ///     An ease-out quadratic curve.
    ///     <code>
    /// ____
    ///     \
    ///      \
    ///       \
    ///        \
    /// </code>
    /// </summary>
    public static float EaseOutQuad(float t)
    {
        return 1 - (1 - t) * (1 - t);
    }

    /// <summary>
    ///     An ease-in-out quadratic curve.
    ///     <code>
    ///    __
    ///   /  \
    ///  /    \
    /// /      \
    /// </code>
    /// </summary>
    public static float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2 * t * t : 1 - MathF.Pow(-2 * t + 2, 2) / 2;
    }

    /// <summary>
    ///     An ease-in cubic curve.
    ///     <code>
    ///        /
    ///       /
    ///      /
    /// _____/
    /// </code>
    /// </summary>
    public static float EaseInCubic(float t)
    {
        return t * t * t;
    }

    /// <summary>
    ///     An ease-out cubic curve.
    ///     <code>
    /// _____
    ///      \
    ///       \
    ///        \
    ///         \
    /// </code>
    /// </summary>
    public static float EaseOutCubic(float t)
    {
        return 1 - MathF.Pow(1 - t, 3);
    }

    /// <summary>
    ///     An ease-in-out cubic curve.
    ///     <code>
    ///    __
    ///   /  \
    ///  /    \
    /// /      \
    /// </code>
    /// </summary>
    public static float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4 * t * t * t : 1 - MathF.Pow(-2 * t + 2, 3) / 2;
    }

    /// <summary>
    ///     An ease-in quartic curve.
    ///     <code>
    ///         /
    ///        /
    ///       /
    /// ______/
    /// </code>
    /// </summary>
    public static float EaseInQuart(float t)
    {
        return t * t * t * t;
    }

    /// <summary>
    ///     An ease-out quartic curve.
    ///     <code>
    /// ______
    ///       \
    ///        \
    ///         \
    ///          \
    /// </code>
    /// </summary>
    public static float EaseOutQuart(float t)
    {
        return 1 - MathF.Pow(1 - t, 4);
    }

    /// <summary>
    ///     An ease-in-out quartic curve.
    ///     <code>
    ///    __
    ///   /  \
    ///  /    \
    /// /      \
    /// </code>
    /// </summary>
    public static float EaseInOutQuart(float t)
    {
        return t < 0.5f ? 8 * t * t * t * t : 1 - MathF.Pow(-2 * t + 2, 4) / 2;
    }

    /// <summary>
    ///     An ease-in quintic curve.
    ///     <code>
    ///          /
    ///         /
    ///        /
    /// _______/
    /// </code>
    /// </summary>
    public static float EaseInQuint(float t)
    {
        return t * t * t * t * t;
    }

    /// <summary>
    ///     An ease-out quintic curve.
    ///     <code>
    /// _______
    ///        \
    ///         \
    ///          \
    ///           \
    /// </code>
    /// </summary>
    public static float EaseOutQuint(float t)
    {
        return 1 - MathF.Pow(1 - t, 5);
    }

    /// <summary>
    ///     An ease-in-out quintic curve.
    ///     <code>
    ///    __
    ///   /  \
    ///  /    \
    /// /      \
    /// </code>
    /// </summary>
    public static float EaseInOutQuint(float t)
    {
        return t < 0.5f ? 16 * t * t * t * t * t : 1 - MathF.Pow(-2 * t + 2, 5) / 2;
    }

    /// <summary>
    ///     An ease-in exponential curve.
    ///     <code>
    ///           /
    ///          /
    ///         /
    /// ________/
    /// </code>
    /// </summary>
    public static float EaseInExpo(float t)
    {
        return t == 0 ? 0 : MathF.Pow(2, 10 * t - 10);
    }

    /// <summary>
    ///     An ease-out exponential curve.
    ///     <code>
    /// ________
    ///         \
    ///          \
    ///           \
    ///            \
    /// </code>
    /// </summary>
    public static float EaseOutExpo(float t)
    {
        return t == 1 ? 1 : 1 - MathF.Pow(2, -10 * t);
    }

    /// <summary>
    ///     An ease-in-out exponential curve.
    ///     <code>
    ///    __
    ///   /  \
    ///  /    \
    /// /      \
    /// </code>
    /// </summary>
    public static float EaseInOutExpo(float t)
    {
        return t == 0 ? 0 :
            t == 1 ? 1 :
            t < 0.5f ? MathF.Pow(2, 20 * t - 10) / 2 : (2 - MathF.Pow(2, -20 * t + 10)) / 2;
    }

    /// <summary>
    ///     An ease-in circular curve.
    ///     <code>
    ///        /
    ///       /
    ///      /
    /// ____/
    /// </code>
    /// </summary>
    public static float EaseInCirc(float t)
    {
        return 1 - MathF.Sqrt(1 - MathF.Pow(t, 2));
    }

    /// <summary>
    ///     An ease-out circular curve.
    ///     <code>
    /// ____
    ///     \
    ///      \
    ///       \
    ///        \
    /// </code>
    /// </summary>
    public static float EaseOutCirc(float t)
    {
        return MathF.Sqrt(1 - MathF.Pow(t - 1, 2));
    }

    /// <summary>
    ///     An ease-in-out circular curve.
    ///     <code>
    ///    __
    ///   /  \
    ///  /    \
    /// /      \
    /// </code>
    /// </summary>
    public static float EaseInOutCirc(float t)
    {
        return t < 0.5f
            ? (1 - MathF.Sqrt(1 - MathF.Pow(2 * t, 2))) / 2
            : (MathF.Sqrt(1 - MathF.Pow(-2 * t + 2, 2)) + 1) / 2;
    }

    /// <summary>
    ///     An ease-in back curve, which overshoots the start.
    ///     <code>
    ///      /
    ///     /
    ///    /
    /// __/
    ///  /
    /// </code>
    /// </summary>
    public static float EaseInBack(float t, float s = 1.70158f)
    {
        return s * t * t * t - s * t * t;
    }

    /// <summary>
    ///     An ease-out back curve, which overshoots the end.
    ///     <code>
    ///   /
    /// _/___
    ///    \
    ///     \
    ///      \
    /// </code>
    /// </summary>
    public static float EaseOutBack(float t, float s = 1.70158f)
    {
        return 1 + s * MathF.Pow(t - 1, 3) + s * MathF.Pow(t - 1, 2);
    }

    /// <summary>
    ///     An ease-in-out back curve, which overshoots at both ends.
    ///     <code>
    ///   /
    /// _/ \_
    ///  /   \
    /// /     \
    /// </code>
    /// </summary>
    public static float EaseInOutBack(float t, float s = 1.70158f)
    {
        return t < 0.5f
            ? (MathF.Pow(2 * t, 2) * ((s * 1.525f + 1) * 2 * t - s * 1.525f)) / 2
            : (MathF.Pow(2 * t - 2, 2) * ((s * 1.525f + 1) * (t * 2 - 2) + s * 1.525f) + 2) / 2;
    }
}
using System;
using System.Runtime.CompilerServices;
namespace WorldGen.Utils;

public static class NullableExtensions
{
    public static T ThrowIfNull<T>(
        this T? input, [CallerArgumentExpression(nameof(input))] string? description = null)
        where T : struct =>
        input ?? ThrowMustNotBeNull<T>(description);

    public static T ThrowIfNull<T>(
        this T? input, [CallerArgumentExpression(nameof(input))] string? description = null)
        where T : class =>
        input ?? ThrowMustNotBeNull<T>(description);

    private static T ThrowMustNotBeNull<T>(string? description) =>
        throw new InvalidOperationException($"{description} must not be null");
}

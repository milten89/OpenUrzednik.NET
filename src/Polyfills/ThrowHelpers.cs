// netstandard2.0 only (src/Directory.Build.props): the argument guard methods .NET has as static members,
// added as C# 14 static extension members so the call sites are the same on every target (ADR-0005).

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

internal static class ArgumentNullExceptionPolyfill
{
    extension(ArgumentNullException)
    {
        public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
                throw new ArgumentNullException(paramName);
        }
    }
}

internal static class ArgumentExceptionPolyfill
{
    extension(ArgumentException)
    {
        public static void ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            ArgumentNullException.ThrowIfNull(argument, paramName);
            if (argument.Length == 0)
                throw new ArgumentException("The value cannot be an empty string.", paramName);
        }

        public static void ThrowIfNullOrWhiteSpace([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            ArgumentNullException.ThrowIfNull(argument, paramName);
            if (string.IsNullOrWhiteSpace(argument))
                throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", paramName);
        }
    }
}

internal static class ArgumentOutOfRangeExceptionPolyfill
{
    extension(ArgumentOutOfRangeException)
    {
        public static void ThrowIfNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} ('{value}') must be a non-negative value.");
        }

        public static void ThrowIfGreaterThan(int value, int other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value > other)
                throw new ArgumentOutOfRangeException(paramName, value, $"{paramName} ('{value}') must be less than or equal to '{other}'.");
        }
    }
}

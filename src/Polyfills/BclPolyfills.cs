// netstandard2.0 only (src/Directory.Build.props): BCL members .NET has, with the .NET names, so call sites need no #if.

using System.Globalization;

namespace System;

internal static class StringPolyfill
{
    internal static bool EndsWith(this string value, char c) => value.Length != 0 && value[value.Length - 1] == c;

    extension(string)
    {
        /// <summary><c>string.Create(IFormatProvider, ref DefaultInterpolatedStringHandler)</c>; the interpolation becomes a <see cref="FormattableString"/> here.</summary>
        public static string Create(IFormatProvider? provider, FormattableString formattable) => formattable.ToString(provider ?? CultureInfo.CurrentCulture);
    }
}

internal static class EnumPolyfill
{
    extension(Enum)
    {
        public static bool IsDefined<TEnum>(TEnum value) where TEnum : struct, Enum => Enum.IsDefined(typeof(TEnum), value);
    }
}

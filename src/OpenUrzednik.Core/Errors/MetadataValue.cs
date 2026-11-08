using System.Globalization;
using System.Text;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Converts a value to its <see cref="OpenUrzednikError.Metadata"/> form (ADR-0021): strings, booleans, <see cref="int"/>,
/// <see cref="long"/> and <see cref="double"/> as they are; other integers and <see cref="float"/> widened (a <see cref="ulong"/> above
/// <see cref="long.MaxValue"/> as a string); <see cref="decimal"/>, <see cref="char"/> and <see cref="Guid"/> as invariant strings; dates,
/// times and durations as ISO 8601 strings; enums by member name; arrays of an allowed type as they are, and other one-dimensional,
/// zero-based arrays as a <c>string[]</c> when every element converts to a string. Anything else (tuples, records, other objects,
/// other arrays) has no form.
/// </summary>
/// <remarks>
/// An enum whose numbers carry a standard meaning (e.g. <c>System.Net.HttpStatusCode</c>) is written as its number by the type that
/// owns the key, not through this converter.
/// </remarks>
internal static class MetadataValue
{
    /// <summary>
    /// Converts a typed value. On .NET the runtime compiles this method once per value type <typeparamref name="T"/>, so the type checks
    /// drop out and a <typeparamref name="T"/> such as <see cref="DateTime"/> is formatted without boxing.
    /// </summary>
    /// <returns><see langword="true"/> when the value has a metadata form.</returns>
    internal static bool TryConvert<T>(T value, out object? converted)
    {
        if (typeof(T) == typeof(DateTime))
        {
            converted = FormatDateTime((DateTime)(object)value!);
            return true;
        }

        if (typeof(T) == typeof(DateTimeOffset))
        {
            converted = FormatDateTimeOffset((DateTimeOffset)(object)value!);
            return true;
        }

        if (typeof(T) == typeof(TimeSpan))
        {
            converted = FormatDuration((TimeSpan)(object)value!);
            return true;
        }

        return TryConvert((object?)value, out converted);
    }

    /// <summary>Converts a value of any type.</summary>
    /// <returns><see langword="true"/> when the value has a metadata form.</returns>
    internal static bool TryConvert(object? value, out object? converted)
    {
        switch (value)
        {
            case null or string or bool or int or long or double:
                converted = value;
                return true;
            case byte number:
                converted = (int)number;
                return true;
            case sbyte number:
                converted = (int)number;
                return true;
            case short number:
                converted = (int)number;
                return true;
            case ushort number:
                converted = (int)number;
                return true;
            case uint number:
                converted = (long)number;
                return true;
            case ulong number:
                converted = number <= long.MaxValue ? (long)number : number.ToString(CultureInfo.InvariantCulture);
                return true;
            case float number:
                converted = (double)number;
                return true;
            // Exact: a JSON number is read as a double in most languages, which would round a decimal with many digits.
            case decimal number:
                converted = number.ToString(CultureInfo.InvariantCulture);
                return true;
            case char character:
                converted = character.ToString();
                return true;
            case Guid guid:
                // Stryker disable once String : equivalent, an empty format is Guid's default "D"
                converted = guid.ToString("D", CultureInfo.InvariantCulture);
                return true;
            case DateTime dateTime:
                converted = FormatDateTime(dateTime);
                return true;
            case DateTimeOffset dateTimeOffset:
                converted = FormatDateTimeOffset(dateTimeOffset);
                return true;
#if NET
            case DateOnly date:
                converted = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                return true;
            case TimeOnly time:
                converted = time.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
                return true;
#endif
            case TimeSpan duration:
                converted = FormatDuration(duration);
                return true;
            case Enum member:
                converted = member.ToString();
                return true;
            case Array array:
                return TryConvertArray(array, out converted);
            default:
                converted = null;
                return false;
        }
    }

    /// <summary>
    /// Formats a duration as ISO 8601, as <c>System.Xml.XmlConvert.ToString(TimeSpan)</c> does: days, hours, minutes and seconds with up
    /// to seven fractional digits, a leading <c>-</c> when negative, and <c>PT0S</c> for zero (e.g. <c>PT30S</c>, <c>P1DT2H</c>,
    /// <c>-PT0.5S</c>). Months and years are never used: they have no fixed length.
    /// </summary>
    internal static string FormatDuration(TimeSpan value)
    {
        var ticks = value.Ticks;
        if (ticks == 0)
            return "PT0S";

        // Unsigned, because negating TimeSpan.MinValue's ticks overflows a long.
        // Stryker disable once Equality : equivalent, zero returned above, so `<= 0` gives the same result
        var negative = ticks < 0;
        var remaining = negative ? (ulong)(-(ticks + 1)) + 1 : (ulong)ticks;
        var days = remaining / TimeSpan.TicksPerDay;
        remaining %= TimeSpan.TicksPerDay;
        var hours = remaining / TimeSpan.TicksPerHour;
        remaining %= TimeSpan.TicksPerHour;
        var minutes = remaining / TimeSpan.TicksPerMinute;
        remaining %= TimeSpan.TicksPerMinute;
        var seconds = remaining / TimeSpan.TicksPerSecond;
        var fraction = remaining % TimeSpan.TicksPerSecond;

        var text = new StringBuilder(32);
        if (negative)
            text.Append('-');
        text.Append('P');
        if (days != 0)
            text.Append(days.ToString(CultureInfo.InvariantCulture)).Append('D');
        if (hours != 0 || minutes != 0 || seconds != 0 || fraction != 0)
        {
            text.Append('T');
            if (hours != 0)
                text.Append(hours.ToString(CultureInfo.InvariantCulture)).Append('H');
            if (minutes != 0)
                text.Append(minutes.ToString(CultureInfo.InvariantCulture)).Append('M');
            if (seconds != 0 || fraction != 0)
            {
                text.Append(seconds.ToString(CultureInfo.InvariantCulture));
                if (fraction != 0)
                    text.Append('.').Append(fraction.ToString("D7", CultureInfo.InvariantCulture).TrimEnd('0'));
                text.Append('S');
            }
        }

        return text.ToString();
    }

    // A date (midnight, no time zone: the library's public dates, ADR-0005) is "yyyy-MM-dd"; any other value keeps its time, and its
    // offset when it has one ("Z" for UTC).
    private static string FormatDateTime(DateTime value)
    {
        if (value.Kind == DateTimeKind.Unspecified && value.TimeOfDay == TimeSpan.Zero)
            return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (value.Kind == DateTimeKind.Utc)
            return value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture);
        if (value.Kind == DateTimeKind.Local)
            return value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture);
        return value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);
    }

    private static string FormatDateTimeOffset(DateTimeOffset value)
        => value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture);

    // Arrays of an allowed type pass as they are; any other one-dimensional, zero-based array becomes a string[] when every element
    // converts to a string (dates, durations, enums, decimals), so the array stays homogeneous. Multi-dimensional and non-zero-based
    // arrays have no form, and neither do nested arrays (so a self-referencing array ends here too).
    private static bool TryConvertArray(Array array, out object? converted)
    {
        if (array is string[] or bool[] or int[] or long[] or double[])
        {
            converted = array;
            return true;
        }

        converted = null;
        if (array.Rank != 1 || array.GetLowerBound(0) != 0)
            return false;

        var strings = new string[array.Length];
        for (var i = 0; i < strings.Length; i++)
        {
            var item = array.GetValue(i);
            if (item is Array)
                return false;

            // Statements, not `out var` in the condition, so that mutation testing can compile its mutants (ADR-0020).
            _ = TryConvert(item, out var element);
            var text = element as string;
            if (text is null)
                return false;

            strings[i] = text;
        }

        converted = strings;
        return true;
    }
}

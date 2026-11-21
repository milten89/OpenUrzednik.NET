using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace OpenUrzednik.MF.WhiteList.Common;

/// <summary>
/// Checks shared by the mappers of the MF White List answers. Each failure is an <see cref="InvalidPayloadException"/> whose message
/// names the field, never its value; the request pipeline returns it as a <c>SerializationError</c>.
/// </summary>
internal static class WhiteListPayload
{
    /// <summary>The format of <c>requestDateTime</c>: Warsaw local time without an offset.</summary>
    internal const string RequestDateTimeFormat = "dd-MM-yyyy HH:mm:ss";

    /// <summary>Throws when the field <paramref name="name"/> is missing or <see langword="null"/>.</summary>
    internal static void EnsurePresent([NotNull] object? value, string name)
    {
        if (value is null)
            throw new InvalidPayloadException($"MF White List API response is missing '{name}'.");
    }

    /// <summary>
    /// The API's <c>requestDateTime</c> (<see cref="RequestDateTimeFormat"/>, Warsaw local time) as an instant with the Warsaw offset; the
    /// hour repeated when daylight saving time ends gets the standard offset <c>+01:00</c> (ADR-0023).
    /// </summary>
    internal static DateTimeOffset ParseRequestDateTime(string? value)
    {
        EnsurePresent(value, "requestDateTime");

        if (!DateTime.TryParseExact(value, RequestDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
            throw new InvalidPayloadException($"MF White List API response has a 'requestDateTime' not in the format {RequestDateTimeFormat}.");

        var instant = WarsawCalendar.ToWarsawOffset(local);
        if (instant is null)
            throw new InvalidPayloadException("MF White List API response has a 'requestDateTime' that doesn't exist in Warsaw (the hour skipped when daylight saving time starts, or before year 1).");

        return instant.Value;
    }
}

using System.Globalization;

namespace OpenUrzednik.Nbp.Extensions;

internal static class DateOnlyExtensions
{
    /// <summary>
    /// Formats the date as an ISO 8601 calendar date (<c>yyyy-MM-dd</c>), the format the NBP API uses in URLs.
    /// The current culture must not leak in: a non-Gregorian calendar (e.g. th-TH) would turn 2026 into 2569.
    /// </summary>
    internal static string ToIso8601String(this DateOnly date)
        => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

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

    /// <summary>
    /// The date as the public API returns it on every target (ADR-0005): a <see cref="DateTime"/> at midnight with
    /// <see cref="DateTimeKind.Unspecified"/>.
    /// </summary>
    internal static DateTime ToPublicDate(this DateOnly date)
#if NET
        => date.ToDateTime(TimeOnly.MinValue);
#else
        => DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
#endif
}

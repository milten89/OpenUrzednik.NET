using System.Globalization;

namespace OpenUrzednik.Dates;

internal static class DateOnlyExtensions
{
    /// <summary>
    /// Formats the date as an ISO 8601 calendar date (<c>yyyy-MM-dd</c>), the format the APIs use in URLs and queries.
    /// The current culture must not leak in: a non-Gregorian calendar (e.g. th-TH) would turn 2026 into 2569.
    /// </summary>
    internal static string ToIso8601String(this DateOnly date)
        => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// The date as the public API returns it on every target (ADR-0005): a <see cref="DateTime"/> at midnight with
    /// <see cref="DateTimeKind.Unspecified"/>.
    /// </summary>
    /// <remarks>
    /// One expression for every target, so there is no <c>#if</c> branch: <c>DayNumber</c> is <c>DateOnly</c>'s own member on .NET and a
    /// polyfill on netstandard2.0, and the <see cref="DateTime"/> built from ticks has <see cref="DateTimeKind.Unspecified"/>. It costs the
    /// same as <c>DateOnly.ToDateTime(TimeOnly.MinValue)</c>, which computes the same ticks.
    /// </remarks>
    internal static DateTime ToPublicDate(this DateOnly date)
        => new(date.DayNumber * TimeSpan.TicksPerDay);
}

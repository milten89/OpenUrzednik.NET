#if !NET
namespace OpenUrzednik.Nbp;

/// <summary>
/// On netstandard2.0, <c>DateOnly</c> is an alias for <see cref="DateTime"/> (see the project file, ADR-0005).
/// These members give it the <c>DateOnly</c> API the code uses, so call sites need no <c>#if</c>.
/// </summary>
internal static class DateOnlyPolyfills
{
    extension(DateTime)
    {
        /// <summary>The date part, with <see cref="DateTimeKind.Unspecified"/>, like <c>DateOnly.FromDateTime</c>.</summary>
        internal static DateTime FromDateTime(DateTime dateTime) => DateTime.SpecifyKind(dateTime.Date, DateTimeKind.Unspecified);
    }

    extension(DateTime date)
    {
        /// <summary>Days since 0001-01-01, like <c>DateOnly.DayNumber</c>.</summary>
        internal int DayNumber => (int)(date.Ticks / TimeSpan.TicksPerDay);
    }
}
#endif

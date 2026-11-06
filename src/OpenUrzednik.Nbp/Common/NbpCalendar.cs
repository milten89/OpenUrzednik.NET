using System.Security;

namespace OpenUrzednik.Nbp.Common;

/// <summary>
/// NBP publishes in Polish time, so "today" and "future" are decided by the date in Europe/Warsaw,
/// not by the caller's local time zone or UTC.
/// </summary>
internal static class NbpCalendar
{
    private static readonly TimeZoneInfo Warsaw = FindWarsaw();

    internal static DateOnly Today(TimeProvider timeProvider)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), Warsaw).DateTime);

    private static TimeZoneInfo FindWarsaw()
    {
        // IANA id on Linux/macOS and on Windows with ICU; Windows id as a fallback.
        string[] ids = ["Europe/Warsaw", "Central European Standard Time"];
        foreach (var id in ids)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // This id isn't on this system: try the next one.
            }
            catch (InvalidTimeZoneException)
            {
                // The data for this id is corrupt: try the next one.
            }
            catch (SecurityException)
            {
                // The time zone data exists but can't be read. This runs in a static initializer, so letting it
                // escape would turn every date method into a TypeInitializationException.
            }
        }

        return CreateCentralEuropeanTime();
    }

    // No readable time zone data (e.g. a minimal container): CET/CEST with the EU rules in force since 1996
    // (UTC+1, UTC+2 from the last Sunday of March 02:00 to the last Sunday of October 03:00 local time).
    internal static TimeZoneInfo CreateCentralEuropeanTime()
    {
        var summerTime = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            DateTime.MinValue.Date,
            DateTime.MaxValue.Date,
            TimeSpan.FromHours(1),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 5, DayOfWeek.Sunday),
            TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 10, 5, DayOfWeek.Sunday));

        return TimeZoneInfo.CreateCustomTimeZone("Europe/Warsaw", TimeSpan.FromHours(1), "Europe/Warsaw", "CET", "CEST", [summerTime]);
    }
}

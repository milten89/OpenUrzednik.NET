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
        foreach (var id in (string[])["Europe/Warsaw", "Central European Standard Time"])
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        // No time zone data (e.g. a minimal container): CET without daylight saving. Off by one hour in summer,
        // which can only matter for a request made between 23:00 and 24:00 UTC.
        return TimeZoneInfo.CreateCustomTimeZone("Europe/Warsaw (UTC+1)", TimeSpan.FromHours(1), "Europe/Warsaw (UTC+1)", "Europe/Warsaw (UTC+1)");
    }
}

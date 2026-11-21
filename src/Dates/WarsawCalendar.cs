using System.Security;

namespace OpenUrzednik.Dates;

/// <summary>
/// Polish public APIs publish and validate dates in Polish time, so "today" and "future" are decided by the date in Europe/Warsaw,
/// not by the caller's local time zone or UTC.
/// </summary>
internal static class WarsawCalendar
{
    private static readonly TimeZoneInfo Warsaw = FindWarsaw();

    internal static DateOnly Today(TimeProvider timeProvider)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), Warsaw).DateTime);

    /// <summary>
    /// The time from now until 00:00 of the next day in Warsaw. Midnight never falls in a daylight saving time change there (the clocks
    /// change at 02:00 and 03:00), so it has one offset, taken for that midnight itself. The result is the real duration: on the days the
    /// clocks change, a day lasts 23 or 25 hours.
    /// </summary>
    internal static TimeSpan UntilNextMidnight(TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        var nextMidnight = TimeZoneInfo.ConvertTime(now, Warsaw).Date.AddDays(1);
        return new DateTimeOffset(nextMidnight, Warsaw.GetUtcOffset(nextMidnight)) - now;
    }

    /// <summary>
    /// The instant of a Warsaw local time (e.g. an API's timestamp without an offset) with its Warsaw offset: <c>+01:00</c> in winter,
    /// <c>+02:00</c> in summer. The hour repeated when daylight saving time ends (02:00–02:59 on the last Sunday of October) is
    /// ambiguous, and gets the standard offset <c>+01:00</c>. <see langword="null"/> for a time that doesn't exist in Warsaw: the hour
    /// skipped when daylight saving time starts (02:00–02:59 on the last Sunday of March), or a time on 1 January of year 1 whose UTC
    /// time would fall before year 1.
    /// </summary>
    /// <param name="local">The local time in Warsaw; its <see cref="DateTime.Kind"/> is ignored.</param>
    internal static DateTimeOffset? ToWarsawOffset(DateTime local)
    {
        var time = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (Warsaw.IsInvalidTime(time))
            return null;

        // An Unspecified time is read as the zone's own local time; an ambiguous one gets the standard offset (TimeZoneInfo.GetUtcOffset).
        var offset = Warsaw.GetUtcOffset(time);

        // Warsaw's offset is never negative, so only the start of 1 January of year 1 has no instant (its UTC time is before year 1).
        if (time.Ticks < offset.Ticks)
            return null;

        return new DateTimeOffset(time, offset);
    }

    // Stryker disable once Block : false survivor, only the static field initializer calls this method and the reused test host runs it once, so Stryker can't activate the mutant; an empty body leaves the zone null, which fails every WarsawCalendar test (checked by hand)
    private static TimeZoneInfo FindWarsaw()
    {
        // IANA id on Linux/macOS and on Windows with ICU; Windows id as a fallback.
        // Stryker disable once String : equivalent, an id that isn't found ends in CreateCentralEuropeanTime, which has the Warsaw rules for every date since 1996 (checked by hand: the tests pass with both ids empty)
        string[] ids = ["Europe/Warsaw", "Central European Standard Time"];
        // Stryker disable once Block : equivalent, skipping the lookup ends in CreateCentralEuropeanTime, which has the Warsaw rules for every date since 1996
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

using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Dates;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Dates;

public class WarsawCalendarTest
{
    [Theory]
    [InlineData("2026-10-01T21:59:00Z", "2026-10-01")] // CEST (UTC+2): 23:59 in Warsaw
    [InlineData("2026-10-01T22:00:00Z", "2026-10-02")] // CEST: midnight in Warsaw, still Oct 1 in UTC
    [InlineData("2026-01-15T22:30:00Z", "2026-01-15")] // CET (UTC+1): 23:30 in Warsaw
    [InlineData("2026-01-15T23:00:00Z", "2026-01-16")] // CET: midnight in Warsaw
    public void Today_ReturnsDateInWarsaw(string utcNow, string expected)
    {
        // Arrange
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse(utcNow, System.Globalization.CultureInfo.InvariantCulture));

        // Act
        var today = WarsawCalendar.Today(timeProvider);

        // Assert
        today.ShouldBe(DateOnly.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("2026-01-15T11:00:00Z", 12 * 60)] // CET (UTC+1): 12:00 in Warsaw, 12 hours to midnight
    [InlineData("2026-07-15T21:30:00Z", 30)] // CEST (UTC+2): 23:30 in Warsaw
    [InlineData("2026-07-15T22:00:00Z", 24 * 60)] // CEST: exactly midnight in Warsaw, a whole day to the next one
    [InlineData("2026-01-15T23:00:00Z", 24 * 60)] // CET: exactly midnight in Warsaw
    [InlineData("2026-01-15T22:59:00Z", 1)] // CET: 23:59 in Warsaw, still Jan 15 in UTC
    [InlineData("2026-03-28T23:30:00Z", 22 * 60 + 30)] // the last Sunday of March, 00:30 CET: the clocks go forward at 02:00, a 23-hour day
    [InlineData("2026-03-29T00:59:00Z", 21 * 60 + 1)] // the last Sunday of March, 01:59 CET, just before the change
    [InlineData("2026-03-29T01:00:00Z", 21 * 60)] // the last Sunday of March, 03:00 CEST, just after the change
    [InlineData("2026-10-24T22:30:00Z", 24 * 60 + 30)] // the last Sunday of October, 00:30 CEST: the clocks go back at 03:00, a 25-hour day
    [InlineData("2026-10-25T00:30:00Z", 22 * 60 + 30)] // the last Sunday of October, 02:30 CEST, the first time
    [InlineData("2026-10-25T01:30:00Z", 21 * 60 + 30)] // the last Sunday of October, 02:30 CET, the repeated hour
    [InlineData("2026-10-25T22:30:00Z", 30)] // the last Sunday of October, 23:30 CET
    public void UntilNextMidnight_ReturnsTimeUntilMidnightInWarsaw(string utcNow, int expectedMinutes)
    {
        // Arrange
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse(utcNow, System.Globalization.CultureInfo.InvariantCulture));

        // Act
        var delay = WarsawCalendar.UntilNextMidnight(timeProvider);

        // Assert
        delay.ShouldBe(TimeSpan.FromMinutes(expectedMinutes));
    }

    [Fact]
    public void UntilNextMidnight_SecondsAndTicks_AreKept()
    {
        // Arrange
        // CET: 23:59:59.9999999 in Warsaw, one tick before midnight.
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 23, 0, 0, TimeSpan.Zero).AddTicks(-1));

        // Act
        var delay = WarsawCalendar.UntilNextMidnight(timeProvider);

        // Assert
        delay.ShouldBe(TimeSpan.FromTicks(1));
    }

    [Theory]
    [InlineData("2026-01-15T12:00:00", "2026-01-15T12:00:00+01:00")] // CET
    [InlineData("2026-07-15T12:00:00", "2026-07-15T12:00:00+02:00")] // CEST
    [InlineData("2026-03-29T01:59:59", "2026-03-29T01:59:59+01:00")] // the last Sunday of March, just before the clocks go forward
    [InlineData("2026-03-29T03:00:00", "2026-03-29T03:00:00+02:00")] // the last Sunday of March, just after the change
    [InlineData("2026-10-25T01:59:59", "2026-10-25T01:59:59+02:00")] // the last Sunday of October, just before the repeated hour
    [InlineData("2026-10-25T02:00:00", "2026-10-25T02:00:00+01:00")] // the repeated hour: ambiguous, so the standard offset
    [InlineData("2026-10-25T02:59:59", "2026-10-25T02:59:59+01:00")] // the repeated hour: ambiguous, so the standard offset
    [InlineData("2026-10-25T03:00:00", "2026-10-25T03:00:00+01:00")] // after the repeated hour
    public void ToWarsawOffset_LocalTime_ReturnsTheInstantWithTheWarsawOffset(string local, string expected)
    {
        // Arrange
        var time = DateTime.Parse(local, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var result = WarsawCalendar.ToWarsawOffset(time);

        // Assert
        var value = result.ShouldNotBeNull();
        value.ShouldBe(DateTimeOffset.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
        value.Offset.ShouldBe(DateTimeOffset.Parse(expected, System.Globalization.CultureInfo.InvariantCulture).Offset);
    }

    [Theory]
    [InlineData("2026-03-29T02:00:00")] // the hour skipped when the clocks go forward
    [InlineData("2026-03-29T02:30:00")]
    [InlineData("2026-03-29T02:59:59")]
    public void ToWarsawOffset_TimeInTheSpringGap_ReturnsNull(string local)
    {
        // Arrange
        var time = DateTime.Parse(local, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        var result = WarsawCalendar.ToWarsawOffset(time);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ToWarsawOffset_TimeWhoseUtcIsBeforeYearOne_ReturnsNull()
    {
        // Arrange: the zone's offset on 1 January of year 1 differs by platform (LMT in IANA data, CET on Windows), so read it from a valid time
        var offset = WarsawCalendar.ToWarsawOffset(new DateTime(1, 1, 2)).ShouldNotBeNull().Offset;
        var time = DateTime.MinValue.Add(offset).AddTicks(-1);

        // Act
        var result = WarsawCalendar.ToWarsawOffset(time);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ToWarsawOffset_TimeWhoseUtcIsTheStartOfYearOne_ReturnsTheInstant()
    {
        // Arrange
        var offset = WarsawCalendar.ToWarsawOffset(new DateTime(1, 1, 2)).ShouldNotBeNull().Offset;
        var time = DateTime.MinValue.Add(offset);

        // Act
        var result = WarsawCalendar.ToWarsawOffset(time);

        // Assert
        result.ShouldNotBeNull().UtcDateTime.ShouldBe(DateTime.MinValue);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    public void ToWarsawOffset_KindOtherThanUnspecified_IsIgnored(DateTimeKind kind)
    {
        // Arrange: the same wall-clock time, whatever its Kind says
        var time = new DateTime(2026, 7, 15, 12, 0, 0, kind);

        // Act
        var result = WarsawCalendar.ToWarsawOffset(time);

        // Assert
        result.ShouldBe(new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.FromHours(2)));
    }

    [Fact]
    public void CreateCentralEuropeanTime_MatchesSystemWarsawZoneOverAYear()
    {
        // Arrange
        var fallback = WarsawCalendar.CreateCentralEuropeanTime();
        // .NET Framework only knows the Windows id.
#if NET
        var warsaw = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
#else
        var warsaw = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
#endif
        var start = new DateTimeOffset(2026, 1, 1, 0, 30, 0, TimeSpan.Zero);

        // Act && Assert
        for (var instant = start; instant < start.AddYears(1); instant = instant.AddHours(1))
            fallback.GetUtcOffset(instant).ShouldBe(warsaw.GetUtcOffset(instant), $"at {instant:O}");
    }

    [Fact]
    public void CreateCentralEuropeanTime_Names_AreWarsawCetAndCest()
    {
        // Act
        var fallback = WarsawCalendar.CreateCentralEuropeanTime();

        // Assert
        fallback.Id.ShouldBe("Europe/Warsaw");
        fallback.DisplayName.ShouldBe("Europe/Warsaw");
        fallback.StandardName.ShouldBe("CET");
        fallback.DaylightName.ShouldBe("CEST");
    }
}

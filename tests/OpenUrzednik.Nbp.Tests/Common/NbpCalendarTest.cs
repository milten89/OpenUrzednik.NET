using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Nbp.Common;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Common;

public class NbpCalendarTest
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
        var today = NbpCalendar.Today(timeProvider);

        // Assert
        today.ShouldBe(DateOnly.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void CreateCentralEuropeanTime_MatchesSystemWarsawZoneOverAYear()
    {
        // Arrange
        var fallback = NbpCalendar.CreateCentralEuropeanTime();
        var warsaw = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        var start = new DateTimeOffset(2026, 1, 1, 0, 30, 0, TimeSpan.Zero);

        // Act && Assert
        for (var instant = start; instant < start.AddYears(1); instant = instant.AddHours(1))
            fallback.GetUtcOffset(instant).ShouldBe(warsaw.GetUtcOffset(instant), $"at {instant:O}");
    }
}

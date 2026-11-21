using OpenUrzednik.MF.WhiteList.Common;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

public sealed class WhiteListPayloadTest
{
    [Fact]
    public void EnsurePresent_Value_DoesNotThrow()
    {
        // Act & Assert
        Should.NotThrow(() => WhiteListPayload.EnsurePresent("x", "requestId"));
    }

    [Fact]
    public void EnsurePresent_Null_ThrowsNamingTheField()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => WhiteListPayload.EnsurePresent(null, "requestId"))
            .Message.ShouldBe("MF White List API response is missing 'requestId'.");
    }

    [Theory]
    [InlineData("20-11-2026 22:44:25", "2026-11-20T22:44:25+01:00")] // CET
    [InlineData("01-07-2026 08:05:09", "2026-07-01T08:05:09+02:00")] // CEST
    [InlineData("25-10-2026 02:30:00", "2026-10-25T02:30:00+01:00")] // the repeated hour: the standard offset
    [InlineData("29-03-2026 03:00:00", "2026-03-29T03:00:00+02:00")] // just after the spring gap
    public void ParseRequestDateTime_WarsawTime_ReturnsTheInstantWithItsOffset(string value, string expected)
    {
        // Act
        var result = WhiteListPayload.ParseRequestDateTime(value);

        // Assert
        var instant = DateTimeOffset.Parse(expected, System.Globalization.CultureInfo.InvariantCulture);
        result.ShouldBe(instant);
        result.Offset.ShouldBe(instant.Offset);
    }

    [Fact]
    public void ParseRequestDateTime_Null_ThrowsMissing()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => WhiteListPayload.ParseRequestDateTime(null))
            .Message.ShouldBe("MF White List API response is missing 'requestDateTime'.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026-11-20 22:44:25")] // ISO order
    [InlineData("20-11-2026 22:44")] // no seconds
    [InlineData("20-11-2026T22:44:25")]
    [InlineData("20-11-2026 22:44:25+01:00")]
    [InlineData("31-11-2026 22:44:25")] // no such day
    public void ParseRequestDateTime_OtherFormat_ThrowsWithoutTheValue(string value)
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => WhiteListPayload.ParseRequestDateTime(value))
            .Message.ShouldBe("MF White List API response has a 'requestDateTime' not in the format dd-MM-yyyy HH:mm:ss.");
    }

    [Fact]
    public void ParseRequestDateTime_TimeInTheSpringGap_Throws()
    {
        // Act & Assert: 02:30 on the last Sunday of March doesn't exist in Warsaw
        Should.Throw<InvalidPayloadException>(() => WhiteListPayload.ParseRequestDateTime("29-03-2026 02:30:00"))
            .Message.ShouldBe("MF White List API response has a 'requestDateTime' that doesn't exist in Warsaw (the hour skipped when daylight saving time starts, or before year 1).");
    }

    [Fact]
    public void ParseRequestDateTime_TimeBeforeYearOneInUtc_Throws()
    {
        // Act & Assert: Warsaw is ahead of UTC, so 00:30 on 1 January of year 1 has no instant (it threw ArgumentOutOfRangeException)
        Should.Throw<InvalidPayloadException>(() => WhiteListPayload.ParseRequestDateTime("01-01-0001 00:30:00"))
            .Message.ShouldBe("MF White List API response has a 'requestDateTime' that doesn't exist in Warsaw (the hour skipped when daylight saving time starts, or before year 1).");
    }

    [Fact]
    public void ParseRequestDateTime_WithAFieldName_ReturnsTheInstant()
    {
        // Act
        var result = WhiteListPayload.ParseRequestDateTime("21-11-2026 19:58:34", WhiteListPayload.ResponseDateTimeField);

        // Assert
        result.ShouldBe(new DateTimeOffset(2026, 11, 21, 19, 58, 34, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void ParseRequestDateTime_NullWithAFieldName_ThrowsNamingThatField()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => WhiteListPayload.ParseRequestDateTime(null, "responseDateTime"))
            .Message.ShouldBe("MF White List API response is missing 'responseDateTime'.");
    }

    [Fact]
    public void ParseRequestDateTime_OtherFormatWithAFieldName_ThrowsNamingThatField()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => WhiteListPayload.ParseRequestDateTime("2026-11-21", "responseDateTime"))
            .Message.ShouldBe("MF White List API response has a 'responseDateTime' not in the format dd-MM-yyyy HH:mm:ss.");
    }

    [Fact]
    public void ParseRequestDateTime_TimeInTheSpringGapWithAFieldName_ThrowsNamingThatField()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => WhiteListPayload.ParseRequestDateTime("29-03-2026 02:30:00", "responseDateTime"))
            .Message.ShouldBe("MF White List API response has a 'responseDateTime' that doesn't exist in Warsaw (the hour skipped when daylight saving time starts, or before year 1).");
    }

    [Theory]
    [InlineData("02-11-2026", 2026, 11, 2)]
    [InlineData("31-12-2020", 2020, 12, 31)]
    [InlineData("01-01-2021", 2021, 1, 1)]
    public void ParseRequestedDate_DayMonthYear_ReturnsMidnightWithUnspecifiedKind(string value, int year, int month, int day)
    {
        // Act
        var result = WhiteListPayload.ParseRequestedDate(value);

        // Assert
        result.ShouldBe(new DateTime(year, month, day));
        result.Kind.ShouldBe(DateTimeKind.Unspecified);
    }

    [Fact]
    public void ParseRequestedDate_Null_ThrowsMissing()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => WhiteListPayload.ParseRequestedDate(null))
            .Message.ShouldBe("MF White List API response is missing 'requestedDate'.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026-11-02")] // ISO order
    [InlineData("2-11-2026")] // no leading zero
    [InlineData("02-11-2026 10:00:00")] // with a time
    [InlineData("31-11-2026")] // no such day
    public void ParseRequestedDate_OtherFormat_ThrowsWithoutTheValue(string value)
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => WhiteListPayload.ParseRequestedDate(value))
            .Message.ShouldBe("MF White List API response has a 'requestedDate' not in the format dd-MM-yyyy.");
    }

    [Fact]
    public void ParseRequestedDate_AnotherCulture_UsesTheInvariantCalendar()
    {
        // Arrange: a culture with another calendar must not shift the year
        using var _ = new OpenUrzednik.TestCommon.CultureScope("th-TH");

        // Act
        var result = WhiteListPayload.ParseRequestedDate("02-11-2026");

        // Assert
        result.Year.ShouldBe(2026);
    }

    [Fact]
    public void ParseRequestDateTime_AnotherCulture_UsesTheInvariantCalendar()
    {
        // Arrange: a culture with another calendar must not shift the year
        using var _ = new OpenUrzednik.TestCommon.CultureScope("th-TH");

        // Act
        var result = WhiteListPayload.ParseRequestDateTime("20-11-2026 22:44:25");

        // Assert
        result.Year.ShouldBe(2026);
    }
}

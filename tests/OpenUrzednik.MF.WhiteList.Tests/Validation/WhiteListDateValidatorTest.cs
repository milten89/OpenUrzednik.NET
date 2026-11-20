using OpenUrzednik.Core.Errors;
using OpenUrzednik.MF.WhiteList.Validation;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Validation;

public sealed class WhiteListDateValidatorTest
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(new DateTime(2026, 11, 20));

    [Theory]
    [InlineData("2021-01-01")] // 1 January five years back
    [InlineData("2024-02-29")]
    [InlineData("2026-11-20")] // today
    public void Validate_DateInRange_ReturnsSuccess(string date)
    {
        // Arrange
        var sut = new WhiteListDateValidator("date", Parse(date), Today);

        // Act
        var result = sut.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_DayBeforeTheRange_ReturnsValidationError()
    {
        // Arrange
        var sut = new WhiteListDateValidator("date", Parse("2020-12-31"), Today);

        // Act
        var result = sut.Validate();

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<DateTime>>();
        error.Message.ShouldBe("'date' should not be earlier than 2021-01-01 (1 January 5 years back, Europe/Warsaw): the list doesn't cover earlier dates.");
        error.RuleName.ShouldBe("whiteListDate");
        error.Name.ShouldBe("date");
        error.Value.ShouldBe(new DateTime(2020, 12, 31));
        error.Value.Kind.ShouldBe(DateTimeKind.Unspecified);
        error.ShouldFollowTheMetadataRule();
    }

    [Fact]
    public void Validate_DayAfterToday_ReturnsValidationError()
    {
        // Arrange
        var sut = new WhiteListDateValidator("date", Parse("2026-11-21"), Today);

        // Act
        var result = sut.Validate();

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<DateTime>>();
        error.Message.ShouldBe("'date' should not be later than today (2026-11-20, Europe/Warsaw).");
        error.RuleName.ShouldBe("whiteListDate");
        error.Name.ShouldBe("date");
        error.Value.ShouldBe(new DateTime(2026, 11, 21));
    }

    [Fact]
    public void Validate_NewYear_MovesTheBound()
    {
        // Arrange: on 1 January 2027 the list reaches back to 1 January 2022
        var newYear = DateOnly.FromDateTime(new DateTime(2027, 1, 1));

        // Act
        var before = new WhiteListDateValidator("date", Parse("2021-12-31"), newYear).Validate();
        var first = new WhiteListDateValidator("date", Parse("2022-01-01"), newYear).Validate();

        // Assert
        before.IsFailure.ShouldBeTrue();
        first.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_TimeOfDay_IsIgnored()
    {
        // Arrange: on netstandard2.0 DateOnly is a DateTime, which can carry a time of day
        var lateToday = DateOnly.FromDateTime(new DateTime(2026, 11, 20));
        var sut = new WhiteListDateValidator("date", lateToday, Today);

        // Act
        var result = sut.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void MinDate_Today_IsFirstJanuaryFiveYearsBack()
    {
        // Act
        var minDate = WhiteListDateValidator.MinDate(Today);

        // Assert
        minDate.ShouldBe(Parse("2021-01-01"));
    }

    private static DateOnly Parse(string date) => DateOnly.FromDateTime(DateTime.Parse(date, System.Globalization.CultureInfo.InvariantCulture));
}

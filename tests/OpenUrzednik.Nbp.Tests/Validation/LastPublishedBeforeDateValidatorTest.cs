using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Validation;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Validation;

public class LastPublishedBeforeDateValidatorTest
{
    private const string RuleName = "lastPublishedBeforeDate";
    private const string PropertyName = "date";
    private static readonly DateOnly MinDate = new(2002, 1, 2);
    private static readonly DateOnly Today = new(2026, 10, 1);

    [Fact]
    public void Constructor_NullPropertyName_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new LastPublishedBeforeDateValidator(null!, Today, MinDate, Today))
            .ParamName.ShouldBe("propertyName");
    }

    [Theory]
    [InlineData(2002, 1, 3)]
    [InlineData(2026, 9, 30)]
    [InlineData(2026, 10, 1)]
    [InlineData(2026, 10, 2)]
    public void Validate_DateFromDayAfterMinDateToTomorrow_ReturnsSuccess(int year, int month, int day)
    {
        // Arrange
        var validator = new LastPublishedBeforeDateValidator(PropertyName, new DateOnly(year, month, day), MinDate, Today);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(2002, 1, 2)]
    [InlineData(2002, 1, 1)]
    [InlineData(1990, 1, 1)]
    public void Validate_DateOnOrBeforeMinDate_ReturnsValidationErrorNamingTheDate(int year, int month, int day)
    {
        // Arrange
        var date = new DateOnly(year, month, day);
        var validator = new LastPublishedBeforeDateValidator(PropertyName, date, MinDate, Today);

        // Act
        var result = validator.Validate();

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<DateTime>>();
        error.Message.ShouldBe("'date' should be later than 2002-01-02.");
        error.Metadata["ruleName"].ShouldBe(RuleName);
        error.Metadata["name"].ShouldBe(PropertyName);
        error.Metadata["value"].ShouldBe(date.ToIso8601String());
        error.Value.ShouldBe(date.ToPublicDate());
        error.Value.Kind.ShouldBe(DateTimeKind.Unspecified);
    }

    [Theory]
    [InlineData(2026, 10, 3)]
    [InlineData(2030, 1, 1)]
    public void Validate_DateAfterTomorrow_ReturnsValidationError(int year, int month, int day)
    {
        // Arrange
        var validator = new LastPublishedBeforeDateValidator(PropertyName, new DateOnly(year, month, day), MinDate, Today);

        // Act
        var result = validator.Validate();

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError<DateTime>>()
            .Message.ShouldBe("'date' should not be later than tomorrow (2026-10-02, Europe/Warsaw).");
    }

    [Fact]
    public void Validate_DateOnMinDate_NonGregorianCulture_FormatsMinDateInvariantly()
    {
        // Arrange
        using var _ = new CultureScope("th-TH");
        var validator = new LastPublishedBeforeDateValidator(PropertyName, MinDate, MinDate, Today);

        // Act
        var result = validator.Validate();

        // Assert
        result.Errors.ShouldHaveSingleItem().Message.ShouldBe("'date' should be later than 2002-01-02.");
    }

    [Theory]
    [InlineData(TableType.A, LastPublishedBeforeDateValidator.DailyWindowDays)]
    [InlineData(TableType.B, LastPublishedBeforeDateValidator.WeeklyWindowDays)]
    public void WindowForTable_Table_ReturnsDaysToReachBack(TableType table, int expected)
    {
        // Act
        var days = LastPublishedBeforeDateValidator.WindowForTable(table);

        // Assert
        days.ShouldBe(expected);
    }

#if !NET
    [Fact]
    public void Validate_TomorrowWithTimeOfDay_ReturnsSuccess()
    {
        // Arrange
        // On netstandard2.0 the date is a DateTime, which can carry a time; only the date part counts.
        var validator = new LastPublishedBeforeDateValidator(PropertyName, new DateTime(2026, 10, 2, 23, 0, 0), MinDate, Today);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
#endif
}

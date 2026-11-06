using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Validation;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Validation;

public class CurrencyDateValidatorTest
{
    private static readonly DateOnly FarFuture = new(2100, 1, 1);

    private const string RuleName = "currencyDate";
    private const string PropertyName = "date";
    private static readonly DateOnly MinDate = new(2002, 1, 2);

    [Fact]
    public void Constructor_NullPropertyName_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new CurrencyDateValidator(null!, MinDate, FarFuture))
            .ParamName.ShouldBe("propertyName");
    }

    [Theory]
    [InlineData(2002, 1, 2)]
    [InlineData(2002, 1, 3)]
    [InlineData(2026, 8, 1)]
    public void Validate_DateOnOrAfterMinDate_ReturnsSuccess(int year, int month, int day)
    {
        // Arrange
        var validator = new CurrencyDateValidator(PropertyName, new DateOnly(year, month, day), FarFuture);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(2002, 1, 1)]
    [InlineData(2001, 12, 31)]
    [InlineData(1990, 1, 1)]
    public void Validate_DateBeforeMinDate_ReturnsValidationError(int year, int month, int day)
    {
        // Arrange
        var date = new DateOnly(year, month, day);
        var validator = new CurrencyDateValidator(PropertyName, date, FarFuture);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Metadata["ruleName"].ShouldBe(RuleName);
        error.Metadata["name"].ShouldBe(PropertyName);
        error.Metadata["value"].ShouldBe(date);
        error.Message.ShouldContain("2002-01-02");
    }

    [Fact]
    public void Name_Always_ReturnsCurrencyDate()
    {
        // Arrange
        var validator = new CurrencyDateValidator(PropertyName, MinDate, FarFuture);

        // Act
        var name = validator.Name;

        // Assert
        name.ShouldBe(RuleName);
    }

    [Fact]
    public void Validate_DateBeforeMinDate_NonGregorianCulture_FormatsMinDateInvariantly()
    {
        // Arrange
        using var _ = new CultureScope("th-TH");
        var validator = new CurrencyDateValidator("date", new DateOnly(2001, 12, 31), FarFuture);

        // Act
        var result = validator.Validate();

        // Assert
        result.Errors.ShouldHaveSingleItem().Message.ShouldBe("'date' should be greater or equal 2002-01-02.");
    }

    [Fact]
    public void Validate_DateIsToday_ReturnsSuccess()
    {
        // Arrange
        var today = new DateOnly(2026, 10, 1);
        var validator = new CurrencyDateValidator("date", today, today);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_DateAfterToday_ReturnsValidationError()
    {
        // Arrange
        var today = new DateOnly(2026, 10, 1);
        var validator = new CurrencyDateValidator("date", today.AddDays(1), today);

        // Act
        var result = validator.Validate();

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Message.ShouldBe("'date' should not be later than today (2026-10-01, Europe/Warsaw).");
    }

#if !NET
    [Fact]
    public void Validate_TodayWithTimeOfDay_ReturnsSuccess()
    {
        // Arrange
        // On netstandard2.0 the date is a DateTime, which can carry a time; only the date part counts.
        var today = new DateTime(2026, 10, 2);
        var validator = new CurrencyDateValidator(PropertyName, today.AddHours(15), today);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
#endif
}

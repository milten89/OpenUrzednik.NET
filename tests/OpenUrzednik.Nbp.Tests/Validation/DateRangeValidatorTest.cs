using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Validation;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Validation;

public class DateRangeValidatorTest
{
    private const string RuleName = "dateRange";
    private const string PropertyName = "date range";
    private const int MaxDateRange = 93;
    private static readonly DateOnly BaseDate = new(2026, 1, 1);

    [Fact]
    public void Validate_SameFromAndToDate_ReturnsSuccess()
    {
        // Arrange
        var validator = new DateRangeValidator((BaseDate, BaseDate), MaxDateRange);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_RangeAtMaxBoundary_ReturnsSuccess()
    {
        // Arrange
        var to = BaseDate.AddDays(MaxDateRange);
        var validator = new DateRangeValidator((BaseDate, to), MaxDateRange);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_RangeOneDayOverMaxBoundary_ReturnsValidationError()
    {
        // Arrange
        var to = BaseDate.AddDays(MaxDateRange + 1);
        var validator = new DateRangeValidator((BaseDate, to), MaxDateRange);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Metadata["ruleName"].ShouldBe(RuleName);
        error.Metadata["name"].ShouldBe(PropertyName);
        error.Message.ShouldContain(MaxDateRange.ToString());
    }

    [Fact]
    public void Validate_FromGreaterThanTo_ReturnsValidationError()
    {
        // Arrange
        var from = BaseDate;
        var to = BaseDate.AddDays(-1);
        var validator = new DateRangeValidator((from, to), MaxDateRange);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Metadata["ruleName"].ShouldBe(RuleName);
        error.Message.ShouldContain("greater than end date");
    }

    [Fact]
    public void Validate_FromGreaterThanToAndOutOfRange_ReturnsFromGreaterThanToErrorFirst()
    {
        // Arrange
        var from = BaseDate;
        var to = BaseDate.AddDays(-(MaxDateRange + 10));
        var validator = new DateRangeValidator((from, to), MaxDateRange);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Message.ShouldContain("greater than end date");
    }

    [Fact]
    public void Validate_RangeAtMaxBoundaryAcrossLeapYearFebruary_ReturnsSuccess()
    {
        // Arrange
        var from = new DateOnly(2024, 1, 1);
        var to = new DateOnly(2024, 4, 3);
        var validator = new DateRangeValidator((from, to), MaxDateRange);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_RangeOneDayOverMaxBoundaryAcrossLeapYearFebruary_ReturnsValidationError()
    {
        // Arrange
        var from = new DateOnly(2024, 1, 1);
        var to = new DateOnly(2024, 4, 4);
        var validator = new DateRangeValidator((from, to), MaxDateRange);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Metadata["ruleName"].ShouldBe(RuleName);
    }

    [Fact]
    public void Name_Always_ReturnsDateRange()
    {
        // Arrange
        var validator = new DateRangeValidator((BaseDate, BaseDate), MaxDateRange);

        // Act
        var name = validator.Name;

        // Assert
        name.ShouldBe(RuleName);
    }

    [Fact]
    public void Validate_FromAfterTo_NonGregorianCulture_FormatsDatesInvariantly()
    {
        // Arrange
        using var _ = new CultureScope("th-TH");
        var validator = new DateRangeValidator((new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1)), MaxDateRange);

        // Act
        var result = validator.Validate();

        // Assert
        result.Errors.ShouldHaveSingleItem().Message.ShouldBe("Start date '2026-02-01' is greater than end date '2026-01-01'.");
    }

    [Fact]
    public void Validate_RangeTooLong_NonGregorianCulture_FormatsDatesInvariantly()
    {
        // Arrange
        using var _ = new CultureScope("th-TH");
        var validator = new DateRangeValidator((new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), MaxDateRange);

        // Act
        var result = validator.Validate();

        // Assert
        result.Errors.ShouldHaveSingleItem().Message.ShouldContain("'2026-01-01 : 2026-12-31'");
    }

    [Theory]
    [InlineData(DateRangeValidator.MaxRatesDateRange)]
    [InlineData(DateRangeValidator.MaxTablesDateRange)]
    public void Validate_RangeAtLimit_ReturnsSuccess(int maxDays)
    {
        // Arrange
        var validator = new DateRangeValidator((BaseDate, BaseDate.AddDays(maxDays)), maxDays);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(DateRangeValidator.MaxRatesDateRange)]
    [InlineData(DateRangeValidator.MaxTablesDateRange)]
    public void Validate_RangeOneDayOverLimit_ReturnsValidationErrorWithLimit(int maxDays)
    {
        // Arrange
        var validator = new DateRangeValidator((BaseDate, BaseDate.AddDays(maxDays + 1)), maxDays);

        // Act
        var result = validator.Validate();

        // Assert
        result.Errors.ShouldHaveSingleItem().Message.ShouldEndWith($"should not exceed {maxDays} days.");
    }
}

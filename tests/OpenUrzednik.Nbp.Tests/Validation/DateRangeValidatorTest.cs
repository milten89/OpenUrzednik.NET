using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Validation;

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
        var validator = new DateRangeValidator((BaseDate, BaseDate));

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
        var validator = new DateRangeValidator((BaseDate, to));

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
        var validator = new DateRangeValidator((BaseDate, to));

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
        var validator = new DateRangeValidator((from, to));

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
        var validator = new DateRangeValidator((from, to));

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
        var validator = new DateRangeValidator((from, to));

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
        var validator = new DateRangeValidator((from, to));

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
        var validator = new DateRangeValidator((BaseDate, BaseDate));

        // Act
        var name = validator.Name;

        // Assert
        name.ShouldBe(RuleName);
    }
}
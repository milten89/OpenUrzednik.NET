using Bogus;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Validation;
using OpenUrzednik.TestCommon.Extensions;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Validation;

public class TopCountValidatorTest
{
    private const string RuleName = "topCount";
    private const string PropertyName = "topCount";

    [Fact]
    public void Constructor_NullPropertyName_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new TopCountValidator(null!, 1))
            .ParamName.ShouldBe("propertyName");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(TopCountValidator.MaxTopCount)]
    public void Validate_PositiveValue_ReturnsSuccess(int value)
    {
        // Arrange
        var validator = new TopCountValidator(PropertyName, value);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_RandomValueWithinLimit_ReturnsSuccess()
    {
        // Arrange
        var value = new Faker().WithConstantSeed().Random.Int(1, TopCountValidator.MaxTopCount);
        var validator = new TopCountValidator(PropertyName, value);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(TopCountValidator.MaxTopCount + 1)]
    [InlineData(int.MaxValue)]
    public void Validate_OutOfRangeValue_ReturnsValidationError(int value)
    {
        // Arrange
        var validator = new TopCountValidator(PropertyName, value);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Metadata["ruleName"].ShouldBe(RuleName);
        error.Metadata["name"].ShouldBe(PropertyName);
        error.Metadata["value"].ShouldBe(value);
    }

    [Theory]
    [InlineData(TopCountValidator.MaxDailyTablesTopCount)]
    [InlineData(TopCountValidator.MaxTableBTopCount)]
    public void Validate_ValueAtCustomMax_ReturnsSuccess(int max)
    {
        // Arrange
        var validator = new TopCountValidator(PropertyName, max, max);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(TopCountValidator.MaxDailyTablesTopCount)]
    [InlineData(TopCountValidator.MaxTableBTopCount)]
    public void Validate_ValueAboveCustomMax_ReturnsValidationErrorNamingTheMax(int max)
    {
        // Arrange
        var validator = new TopCountValidator(PropertyName, max + 1, max);

        // Act
        var result = validator.Validate();

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>()
            .Message.ShouldBe($"'{PropertyName}' should be between 1 and {max}.");
    }

    [Theory]
    [InlineData(TableType.A, 67)]
    [InlineData(TableType.B, 14)]
    [InlineData((TableType)99, 67)] // An undefined value is reported by TableTypeValidator; this must not throw.
    public void MaxForTable_Table_ReturnsTheApiLimit(TableType table, int expected)
    {
        // Act
        var max = TopCountValidator.MaxForTable(table);

        // Assert
        max.ShouldBe(expected);
    }

    [Fact]
    public void Name_Always_ReturnsTopCount()
    {
        // Arrange
        var validator = new TopCountValidator(PropertyName, 1);

        // Act
        var name = validator.Name;

        // Assert
        name.ShouldBe(RuleName);
    }
}

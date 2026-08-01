using Bogus;

using OpenUrzednik.Core.Errors;
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
        // Act
        var exception = Record.Exception(() => new TopCountValidator(null!, 1));
        exception.ShouldBeOfType<ArgumentNullException>()
            .ParamName.ShouldBe("propertyName");
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(int.MaxValue)]
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
    public void Validate_RandomPositiveValue_ReturnsSuccess()
    {
        // Arrange
        var value = new Faker().WithConstantSeed().Random.Int(1);
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
    public void Validate_ZeroOrNegativeValue_ReturnsValidationError(int value)
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
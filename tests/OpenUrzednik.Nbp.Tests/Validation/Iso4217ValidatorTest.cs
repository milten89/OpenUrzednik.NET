using Bogus;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Validation;

using Shouldly;

namespace OpenUrzednik.Nbp.Tests.Validation;

public class Iso4217ValidatorTest
{
    private const string RuleName = "iso4217";
    private const string PropertyName = "currency";
    private static readonly Faker Faker = new();

    [Fact]
    public void Constructor_NullPropertyName_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => new Iso4217Validator(null!, ""))
            .ParamName.ShouldBe("propertyName");
    }
    
    [Theory]
    [InlineData("USD")]
    [InlineData("eur")]
    [InlineData("GbP")]
    public void Validate_ValidThreeLetterCode_ReturnsSuccess(string currency)
    {
        // Arrange
        var validator = new Iso4217Validator(PropertyName, currency);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_RandomThreeLetterAlphaCode_ReturnsSuccess()
    {
        // Arrange
        var currency = Faker.Random.String2(3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        var validator = new Iso4217Validator(PropertyName, currency);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NullEmptyOrWhitespaceValue_ReturnsValidationError(string? currency)
    {
        // Arrange
        var validator = new Iso4217Validator(PropertyName, currency!);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Metadata["ruleName"].ShouldBe(RuleName);
        error.Message.ShouldContain("null, empty or whitespace");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("US1")]
    [InlineData("12A")]
    [InlineData("US-D")]
    [InlineData("USD\n")]
    [InlineData(" USD")]
    [InlineData("USD ")]
    public void Validate_InvalidFormatValue_ReturnsValidationError(string currency)
    {
        // Arrange
        var validator = new Iso4217Validator(PropertyName, currency);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<ValidationError>();
        error.Metadata["ruleName"].ShouldBe(RuleName);
        error.Metadata["value"].ShouldBe(currency);
        error.Message.ShouldContain("not proper ISO 4217 currency code");
    }

    [Fact]
    public void Name_Always_ReturnsIso4217()
    {
        // Arrange
        var validator = new Iso4217Validator(PropertyName, "USD");

        // Act
        var name = validator.Name;

        // Assert
        name.ShouldBe(RuleName);
    }
}
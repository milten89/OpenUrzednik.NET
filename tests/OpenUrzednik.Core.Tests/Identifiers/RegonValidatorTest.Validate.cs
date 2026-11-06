using OpenUrzednik.Identifiers;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Identifiers;

public partial class RegonValidatorTest
{
    private const string PropertyName = "regon";

    // Fake numbers with valid check digits: 123456785 (sum 192 mod 11 = 5), 987654326 (sum 248 mod 11 = 6),
    // 12345678512347 (parent 123456785, unit 1234, sum 260 mod 11 = 7), 12345678500010 (sum 219 mod 11 = 10, so 0).
    [Theory]
    [InlineData("123456785", "123456785")]
    [InlineData("987654326", "987654326")]
    [InlineData("12345678512347", "12345678512347")]
    [InlineData("12345678500010", "12345678500010")]
    [InlineData("123-456-785", "123456785")]
    [InlineData("123456785 1234 7", "12345678512347")]
    public void Validate_ValidRegon_ReturnsSuccessAndNormalizedDigits(string value, string normalized)
    {
        // Arrange
        var validator = new RegonValidator(PropertyName, value);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        validator.Normalized.ShouldBe(normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_NullEmptyOrWhitespace_ReturnsValidationError(string? value)
    {
        // Act
        var result = new RegonValidator(PropertyName, value!).Validate();

        // Assert
        result.ShouldBeMaskedValidationError("regon", PropertyName, value, "is empty or whitespace.");
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("1234567850")]
    [InlineData("1234567851234")]
    [InlineData("123456785123470")]
    [InlineData("12345678S")]
    [InlineData("PL123456785")]
    public void Validate_WrongLengthOrCharacters_ReturnsValidationError(string value)
    {
        // Act
        var result = new RegonValidator(PropertyName, value).Validate();

        // Assert
        result.ShouldBeMaskedValidationError("regon", PropertyName, value, "must be a REGON of 9 or 14 digits.");
    }

    [Theory]
    [InlineData("123456784")]
    [InlineData("12345678512348")]
    [InlineData("12345678412341")] // the 14th digit fits, but the parent REGON 123456784 doesn't (its check digit is 5)
    public void Validate_WrongCheckDigit_ReturnsValidationError(string value)
    {
        // Act
        var result = new RegonValidator(PropertyName, value).Validate();

        // Assert
        result.ShouldBeMaskedValidationError("regon", PropertyName, value, "has an invalid check digit.");
    }

    [Theory]
    [InlineData("000000000")]
    [InlineData("00000000000000")]
    public void Validate_AllZeros_ReturnsValidationError(string value)
    {
        // Act
        var result = new RegonValidator(PropertyName, value).Validate();

        // Assert
        result.ShouldBeMaskedValidationError("regon", PropertyName, value, "isn't an issued number.");
    }
}

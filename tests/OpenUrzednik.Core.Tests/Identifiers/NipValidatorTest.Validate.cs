using OpenUrzednik.Identifiers;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Identifiers;

public partial class NipValidatorTest
{
    private const string PropertyName = "nip";

    // Fake numbers with valid check digits: 1234563218 (sum 118 mod 11 = 8), 2234567895 (the OECD sample), 9876543210 (sum 220, mod 11 = 0).
    [Theory]
    [InlineData("1234563218", "1234563218")]
    [InlineData("2234567895", "2234567895")]
    [InlineData("9876543210", "9876543210")]
    [InlineData("123-456-32-18", "1234563218")]
    [InlineData("123-45-63-218", "1234563218")]
    [InlineData(" 123 456 32 18 ", "1234563218")]
    [InlineData("PL1234563218", "1234563218")]
    [InlineData("pl 123-456-32-18", "1234563218")]
    [InlineData("P-L 123-456-32-18", "1234563218")]
    public void Validate_ValidNip_ReturnsSuccessAndNormalizedDigits(string value, string normalized)
    {
        // Arrange
        var validator = new NipValidator(PropertyName, value);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        validator.Normalized.ShouldBe(normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NullEmptyOrWhitespace_ReturnsValidationError(string? value)
    {
        // Act
        var result = new NipValidator(PropertyName, value!).Validate();

        // Assert
        result.ShouldBeMaskedValidationError("nip", PropertyName, value, "is empty or whitespace.");
    }

    [Theory]
    [InlineData("123456321")]
    [InlineData("12345632180")]
    [InlineData("12345632I8")]
    [InlineData("1234563218PL")]
    [InlineData("--")]
    [InlineData("\uFF11\uFF12\uFF13\uFF14\uFF15\uFF16\uFF13\uFF12\uFF11\uFF18")] // fullwidth digits: only ASCII digits count
    [InlineData("\u0661\u0662\u0663\u0664\u0665\u0666\u0663\u0662\u0661\u0668")] // Arabic-Indic digits
    [InlineData("123\u2013456\u201332\u201318")] // en dashes aren't separators
    public void Validate_WrongLengthOrCharacters_ReturnsValidationError(string value)
    {
        // Act
        var result = new NipValidator(PropertyName, value).Validate();

        // Assert
        result.ShouldBeMaskedValidationError("nip", PropertyName, value, "must be a NIP of 10 digits.");
    }

    [Theory]
    [InlineData("1234563217")]
    [InlineData("1234567890")] // remainder 10: never valid
    [InlineData("PL123-456-32-19")]
    public void Validate_WrongCheckDigit_ReturnsValidationError(string value)
    {
        // Act
        var result = new NipValidator(PropertyName, value).Validate();

        // Assert
        result.ShouldBeMaskedValidationError("nip", PropertyName, value, "has an invalid check digit.");
    }

    [Fact]
    public void Validate_AllZeros_ReturnsValidationError()
    {
        // Act
        var result = new NipValidator(PropertyName, "0000000000").Validate();

        // Assert
        result.ShouldBeMaskedValidationError("nip", PropertyName, "0000000000", "isn't an issued number.");
    }

    [Theory]
    [InlineData("DE1234563218")]
    [InlineData("be 0123456789")]
    public void Validate_ForeignCountryPrefix_ReturnsValidationError(string value)
    {
        // Act
        var result = new NipValidator(PropertyName, value).Validate();

        // Assert
        result.ShouldBeMaskedValidationError("nip", PropertyName, value, "must be a Polish number: the only accepted country prefix is PL.");
    }
}

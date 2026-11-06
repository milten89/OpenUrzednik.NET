using OpenUrzednik.Identifiers;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Identifiers;

public partial class KrsNumberValidatorTest
{
    private const string PropertyName = "krs";

    [Theory]
    [InlineData("0000012345", "0000012345")]
    [InlineData("1234567890", "1234567890")]
    [InlineData("0000-012-345", "0000012345")]
    public void Validate_TenDigits_ReturnsSuccessAndNormalizedDigits(string value, string normalized)
    {
        // Arrange
        var validator = new KrsNumberValidator(PropertyName, value);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        validator.Normalized.ShouldBe(normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_NullEmptyOrWhitespace_ReturnsValidationError(string? value)
    {
        // Act
        var result = new KrsNumberValidator(PropertyName, value!).Validate();

        // Assert
        result.ShouldBeMaskedValidationError("krs", PropertyName, value, "is empty or whitespace.");
    }

    [Theory]
    [InlineData("12345")] // leading zeros are required
    [InlineData("00000123456")]
    [InlineData("00000I2345")]
    [InlineData("PL0000012345")]
    public void Validate_WrongLengthOrCharacters_ReturnsValidationError(string value)
    {
        // Act
        var result = new KrsNumberValidator(PropertyName, value).Validate();

        // Assert
        result.ShouldBeMaskedValidationError("krs", PropertyName, value, "must be a KRS number of 10 digits, leading zeros included.");
    }

    [Fact]
    public void Validate_AllZeros_ReturnsValidationError()
    {
        // Act
        var result = new KrsNumberValidator(PropertyName, "0000000000").Validate();

        // Assert
        result.ShouldBeMaskedValidationError("krs", PropertyName, "0000000000", "isn't an issued number.");
    }
}

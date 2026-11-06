using OpenUrzednik.Identifiers;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Identifiers;

public partial class BankAccountNumberValidatorTest
{
    private const string PropertyName = "accountNumber";

    // Fake numbers: settlement numbers 12345676 and 11111111 have valid check digits, and the IBAN check digits 69 and 55 fit.
    [Theory]
    [InlineData("69123456761234567890123456", "69123456761234567890123456")]
    [InlineData("55111111111234567890123456", "55111111111234567890123456")]
    [InlineData("69 1234 5676 1234 5678 9012 3456", "69123456761234567890123456")]
    [InlineData("PL69123456761234567890123456", "69123456761234567890123456")]
    [InlineData("pl69 1234 5676 1234 5678 9012 3456", "69123456761234567890123456")]
    public void Validate_ValidAccountNumber_ReturnsSuccessAndNormalizedDigits(string value, string normalized)
    {
        // Arrange
        var validator = new BankAccountNumberValidator(PropertyName, value);

        // Act
        var result = validator.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        validator.Normalized.ShouldBe(normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("\t")]
    public void Validate_NullEmptyOrWhitespace_ReturnsValidationErrorAndEmptyNormalized(string? value)
    {
        // Arrange
        var validator = new BankAccountNumberValidator(PropertyName, value!);

        // Act
        var result = validator.Validate();

        // Assert
        result.ShouldBeMaskedValidationError("nrb", PropertyName, value, "is empty or whitespace.");
        validator.Normalized.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("6912345676123456789012345")]
    [InlineData("691234567612345678901234560")]
    [InlineData("69123456761234567890I23456")]
    public void Validate_WrongLengthOrCharacters_ReturnsValidationError(string value)
    {
        // Act
        var result = new BankAccountNumberValidator(PropertyName, value).Validate();

        // Assert
        result.ShouldBeMaskedValidationError("nrb", PropertyName, value, "must be a bank account number of 26 digits (NRB), optionally with the IBAN prefix PL.");
    }

    [Theory]
    [InlineData("68123456761234567890123456")] // wrong IBAN check digits
    [InlineData("69123456761234567890123457")] // a changed account digit
    [InlineData("00000000000000000000000000")] // all zeros fails mod 97
    public void Validate_WrongIbanCheckDigits_ReturnsValidationError(string value)
    {
        // Act
        var result = new BankAccountNumberValidator(PropertyName, value).Validate();

        // Assert
        result.ShouldBeMaskedValidationError("nrb", PropertyName, value, "has an invalid check digit.");
    }

    [Fact]
    public void Validate_WrongSettlementCheckDigit_ReturnsValidationError()
    {
        // Arrange
        // Settlement number 12345677 (its check digit should be 6), with IBAN check digits recomputed so that mod 97 passes.
        const string value = "44123456771234567890123456";

        // Act
        var result = new BankAccountNumberValidator(PropertyName, value).Validate();

        // Assert
        result.ShouldBeMaskedValidationError("nrb", PropertyName, value, "has an invalid check digit.");
    }

    [Theory]
    [InlineData("DE89370400440532013000")]
    [InlineData("DE69123456761234567890123456")]
    public void Validate_ForeignCountryPrefix_ReturnsValidationError(string value)
    {
        // Act
        var result = new BankAccountNumberValidator(PropertyName, value).Validate();

        // Assert
        result.ShouldBeMaskedValidationError("nrb", PropertyName, value, "must be a Polish number: the only accepted country prefix is PL.");
    }
}

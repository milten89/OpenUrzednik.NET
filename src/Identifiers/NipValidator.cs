using System.Diagnostics.CodeAnalysis;

using OpenUrzednik.Core;

namespace OpenUrzednik.Identifiers;

/// <summary>
/// Validates a NIP: 10 digits, written with or without spaces and hyphens, optionally with the EU VAT prefix <c>PL</c>.
/// The 10th digit is the weighted sum of the first nine (weights 6, 5, 7, 2, 3, 4, 5, 6, 7) mod 11; a remainder of 10 is never
/// valid. All zeros passes the sum but is never issued.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Shared source (Identifiers.props): every provider compiles all validators but may use only some.")]
internal sealed class NipValidator(string propertyName, string value) : IdentifierValidator(propertyName, value, allowCountryPrefix: true)
{
    private static readonly int[] Weights = [6, 5, 7, 2, 3, 4, 5, 6, 7];

    /// <inheritdoc />
    public override string Name => "nip";

    /// <inheritdoc />
    protected override string Format => "a NIP of 10 digits";

    /// <inheritdoc />
    protected override bool HasValidLength(int length) => length == 10;

    /// <inheritdoc />
    protected override OpenUrzednikResult ValidateDigits(string digits)
    {
        if (WeightedSum(digits, 0, Weights) % 11 != Digit(digits, 9))
            return InvalidCheckDigit();

        return AreZeros(digits, 10) ? NotIssued() : OpenUrzednikResult.Success();
    }
}

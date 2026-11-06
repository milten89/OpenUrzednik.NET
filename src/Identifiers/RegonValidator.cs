using System.Diagnostics.CodeAnalysis;

using OpenUrzednik.Core;

namespace OpenUrzednik.Identifiers;

/// <summary>
/// Validates a REGON: 9 digits, or 14 for a local unit (the parent unit's 9-digit REGON, a 4-digit unit number and a check digit;
/// Dz.U. 2015 poz. 2009 § 12), written with or without spaces and hyphens. Check digits: weights 8, 9, 2, 3, 4, 5, 6, 7 over the
/// first 8 digits, and 2, 4, 8, 5, 0, 9, 7, 3, 6, 1, 2, 4, 8 over the first 13; the sum mod 11, where 10 counts as 0.
/// A 14-digit REGON must also start with a valid 9-digit one. An all-zero parent REGON is never issued.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Shared source (Identifiers.props): every provider compiles all validators but may use only some.")]
internal sealed class RegonValidator(string propertyName, string value) : IdentifierValidator(propertyName, value, allowCountryPrefix: false)
{
    private static readonly int[] Weights9 = [8, 9, 2, 3, 4, 5, 6, 7];
    private static readonly int[] Weights14 = [2, 4, 8, 5, 0, 9, 7, 3, 6, 1, 2, 4, 8];

    /// <inheritdoc />
    public override string Name => "regon";

    /// <inheritdoc />
    protected override string Format => "a REGON of 9 or 14 digits";

    /// <inheritdoc />
    protected override bool HasValidLength(int length) => length is 9 or 14;

    /// <inheritdoc />
    protected override OpenUrzednikResult ValidateDigits(string digits)
    {
        if (CheckDigit(digits, Weights9) != Digit(digits, 8))
            return InvalidCheckDigit();
        if (digits.Length == 14 && CheckDigit(digits, Weights14) != Digit(digits, 13))
            return InvalidCheckDigit();

        return AreZeros(digits, 9) ? NotIssued() : OpenUrzednikResult.Success();
    }

    private static int CheckDigit(string digits, int[] weights)
    {
        var remainder = WeightedSum(digits, 0, weights) % 11;
        return remainder == 10 ? 0 : remainder;
    }
}

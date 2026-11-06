using System.Diagnostics.CodeAnalysis;

using OpenUrzednik.Core;

namespace OpenUrzednik.Identifiers;

/// <summary>
/// Validates a Polish bank account number (NRB): 26 digits, written with or without spaces and hyphens, optionally in the IBAN form
/// with the prefix <c>PL</c>. Two checks: the IBAN check digits (ISO 13616, MOD 97-10 with the country code <c>PL</c>: the digits
/// after the first two, then <c>2521</c>, then the first two, mod 97 must be 1), and the check digit of the 8-digit settlement number
/// (digits 3 to 10, weights 3, 9, 7, 1, 3, 9, 7, 1, sum mod 10 must be 0; Dz.U. 2024 poz. 1142, annex 2).
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Shared source (Identifiers.props): every provider compiles all validators but may use only some.")]
internal sealed class BankAccountNumberValidator(string propertyName, string value) : IdentifierValidator(propertyName, value, allowCountryPrefix: true)
{
    private static readonly int[] SettlementWeights = [3, 9, 7, 1, 3, 9, 7, 1];

    /// <inheritdoc />
    public override string Name => "nrb";

    /// <inheritdoc />
    protected override string Format => "a bank account number of 26 digits (NRB), optionally with the IBAN prefix PL";

    /// <inheritdoc />
    protected override bool HasValidLength(int length) => length == 26;

    /// <inheritdoc />
    protected override OpenUrzednikResult ValidateDigits(string digits)
    {
        if (Mod97(digits) != 1 || WeightedSum(digits, 2, SettlementWeights) % 10 != 0)
            return InvalidCheckDigit();

        return OpenUrzednikResult.Success();
    }

    // The IBAN number (digits 3-26, "2521" for PL, digits 1-2) mod 97, one digit at a time, without building it.
    private static int Mod97(string digits)
    {
        var remainder = 0;
        for (var i = 2; i < digits.Length; i++)
            remainder = (remainder * 10 + Digit(digits, i)) % 97;
        remainder = (remainder * 10000 + 2521) % 97;
        remainder = (remainder * 100 + Digit(digits, 0) * 10 + Digit(digits, 1)) % 97;
        return remainder;
    }
}

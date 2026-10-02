using System.Text.RegularExpressions;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;

namespace OpenUrzednik.Nbp.Validation;

internal sealed partial class Iso4217Validator(string propertyName, string value) : ValueValidator<string>(propertyName, value)
{
    private const string Iso4217CurrencyCodePattern = @"\A[a-zA-Z]{3}\z";

#if NET
    [GeneratedRegex(Iso4217CurrencyCodePattern, RegexOptions.CultureInvariant)]
    private static partial Regex Iso4217CurrencyCodeRegex();
#else
    // netstandard2.0 has no regex source generator.
    private static readonly Regex Iso4217CurrencyCode = new(Iso4217CurrencyCodePattern, RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static Regex Iso4217CurrencyCodeRegex() => Iso4217CurrencyCode;
#endif

    public override string Name => "iso4217";

    public override OpenUrzednikResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Value))
            return GetValidationErrorResult($"'{PropertyName}' is null, empty or whitespace.");

        if (!Iso4217CurrencyCodeRegex().IsMatch(Value))
            return GetValidationErrorResult($"'{Value}' is not proper ISO 4217 currency code.");

        return OpenUrzednikResult.Success();
    }
}

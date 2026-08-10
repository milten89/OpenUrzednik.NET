using System.Text.RegularExpressions;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;

namespace OpenUrzednik.Nbp.Validation;

internal sealed partial class Iso4217Validator(string propertyName, string value) : ValueValidator<string>(propertyName, value)
{
    [GeneratedRegex(@"\A[a-zA-Z]{3}\z", RegexOptions.CultureInvariant)]
    private static partial Regex Iso4217CurrencyCodeRegex();

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

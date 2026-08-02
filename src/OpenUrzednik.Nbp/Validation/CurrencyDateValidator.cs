using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;

namespace OpenUrzednik.Nbp.Validation;

internal sealed class CurrencyDateValidator(string propertyName, DateOnly value) : ValueValidator<DateOnly>(propertyName, value)
{
    public static readonly DateOnly MinDate = new(2002, 1, 2);

    public override string Name => "currencyDate";

    public override OpenUrzednikResult Validate()
    {
        return Value >= MinDate
            ? OpenUrzednikResult.Success()
            : GetValidationErrorResult($"'{PropertyName}' should be greater or equal {MinDate:d}.");
    }
}

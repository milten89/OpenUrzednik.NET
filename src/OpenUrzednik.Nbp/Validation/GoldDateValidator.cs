using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;

namespace OpenUrzednik.Nbp.Validation;

internal sealed class GoldDateValidator(string propertyName, DateOnly value) : ValueValidator<DateOnly>(propertyName, value)
{
    private static readonly DateOnly MinDate = new(2013, 1, 2);

    public override string Name => "goldDate";

    public override OpenUrzednikResult Validate()
    {
        return Value >= MinDate
            ? OpenUrzednikResult.Success()
            : GetValidationErrorResult($"'{PropertyName}' should be greater or equal {MinDate:d}.");
    }
}

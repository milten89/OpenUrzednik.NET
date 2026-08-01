using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;

namespace OpenUrzednik.Nbp.Validation;

internal sealed class TopCountValidator(string propertyName, int value) : ValueValidator<int>(propertyName, value)
{
    public override string Name => "topCount";

    public override OpenUrzednikResult Validate()
    {
        return Value > 0
            ? OpenUrzednikResult.Success()
            : GetValidationErrorResult($"'{PropertyName}' should be greater or equal 1.");
    }
}

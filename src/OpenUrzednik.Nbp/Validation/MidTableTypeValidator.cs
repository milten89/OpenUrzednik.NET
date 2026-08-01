using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;
using OpenUrzednik.Nbp.Table;

namespace OpenUrzednik.Nbp.Validation;

internal sealed class MidTableTypeValidator(string propertyName, MidTableType value) : ValueValidator<MidTableType>(propertyName, value)
{
    public override string Name => "midTableType";

    public override OpenUrzednikResult Validate()
    {
        return Enum.IsDefined(Value)
            ? OpenUrzednikResult.Success()
            : GetValidationErrorResult($"'{PropertyName}' has value not defined by {nameof(MidTableType)}.");
    }
}

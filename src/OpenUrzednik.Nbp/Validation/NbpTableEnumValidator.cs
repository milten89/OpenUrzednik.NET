using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.UrlBuilder;

namespace OpenUrzednik.Nbp.Validation;

internal sealed class NbpTableEnumValidator(string propertyName, NbpTable value) : ValueValidator<NbpTable>(propertyName, value)
{
    public override string Name => "nbpTable";

    public override OpenUrzednikResult Validate()
    {
        return Enum.IsDefined(Value)
            ? OpenUrzednikResult.Success()
            : GetValidationErrorResult($"'{PropertyName}' has value not defined by {nameof(NbpTable)}.");
    }
}

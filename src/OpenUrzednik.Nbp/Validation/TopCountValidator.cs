using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;

namespace OpenUrzednik.Nbp.Validation;

internal sealed class TopCountValidator(string propertyName, int value) : ValueValidator<int>(propertyName, value)
{
    /// <summary>The NBP API rejects <c>last/{n}</c> with more than 255 results.</summary>
    public const int MaxTopCount = 255;

    public override string Name => "topCount";

    public override OpenUrzednikResult Validate()
    {
        return Value is >= 1 and <= MaxTopCount
            ? OpenUrzednikResult.Success()
            : GetValidationErrorResult($"'{PropertyName}' should be between 1 and {MaxTopCount}.");
    }
}

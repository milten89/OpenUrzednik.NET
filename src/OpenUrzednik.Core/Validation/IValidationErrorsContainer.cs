using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Validation;

public interface IValidationErrorsContainer
{
    public IReadOnlyList<ValidationError> Errors { get; }
}

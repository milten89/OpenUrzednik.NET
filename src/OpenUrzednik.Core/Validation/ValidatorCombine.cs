using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Validation;

public static class ValidatorCombine
{
    public static OpenUrzednikResult ToResult(params IValidationErrorsContainer[] errorContainers)
    {
        var errors = new List<ValidationError>();
        foreach (var errorContainer in errorContainers) 
            errors.AddRange(errorContainer.Errors);

        return errors.Count == 0 
            ? OpenUrzednikResult.Success() 
            : OpenUrzednikResult.Failure(errors);
    }

    public static OpenUrzednikResult ToResult<T>(this ValueValidator<T> validator) 
        => validator.IsValid 
               ? OpenUrzednikResult.Success() 
               : OpenUrzednikResult.Failure(validator.Errors);
}

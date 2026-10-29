using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core;

/// <summary>
/// Builds the exception that <c>EnsureSuccess</c> throws for a failed result.
/// </summary>
internal static class ResultExceptions
{
    // ADR-0002: one error -> its exception; several validation errors -> one OpenUrzednikValidationException listing them;
    // several errors of other kinds -> the first error's exception. Errors always holds every error.
    internal static OpenUrzednikException Create(IReadOnlyList<OpenUrzednikError> errors)
    {
        if (errors.Count == 1)
            return errors[0].ToException();

        if (errors.All(e => e is ValidationError))
            return new OpenUrzednikValidationException([.. errors.Cast<ValidationError>()]);

        var exception = errors[0].ToException();
        exception.SetErrors(errors[0], errors);
        return exception;
    }
}

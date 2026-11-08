using System.Text;

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
        // A local, not `errors.Count` in the comparisons, so that mutation testing checks them (ADR-0020):
        // Stryker's LINQ mutator turns the property into `errors.Sum`, which can't compile, and discards the mutants around it.
        var count = errors.Count;
        if (count == 1)
            return errors[0].ToException();

        if (errors.All(e => e is ValidationError))
            return new OpenUrzednikValidationException([.. errors.Cast<ValidationError>()]);

        var exception = errors[0].ToException();
        exception.SetErrors(errors[0], errors);
        return exception;
    }

    // OpenUrzednikResult<T>.Value on a failure: an InvalidOperationException, as Nullable<T>.Value throws, naming the errors.
    internal static InvalidOperationException CreateValueOfFailure(IReadOnlyList<OpenUrzednikError> errors)
    {
        var message = new StringBuilder("Cannot access the value of a failed result: ");
        // A local, not `errors.Count` in the condition: see Create (ADR-0020).
        var count = errors.Count;
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
                message.Append("; ");
            message.Append(errors[i].Code).Append(": ").Append(errors[i].Message);
        }

        return new InvalidOperationException(message.ToString(), errors[0].ToException());
    }
}

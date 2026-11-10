using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Nbp.Validation;

/// <summary>
/// Failures of the date validators. Public dates are <see cref="DateTime"/> on every target (ADR-0005), so the errors carry
/// <see cref="ValidationError{T}"/> of <see cref="DateTime"/>, never the internal <c>DateOnly</c> (ADR-0021).
/// </summary>
internal static class DateValidation
{
    internal static OpenUrzednikResult Failure(string message, string ruleName, string propertyName, DateOnly value)
        => OpenUrzednikResult.Failure(new ValidationError<DateTime>(message, ruleName, propertyName, value.ToPublicDate()));
}

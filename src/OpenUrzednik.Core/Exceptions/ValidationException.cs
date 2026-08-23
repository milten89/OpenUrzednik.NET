using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a validation error occurs.
/// </summary>
public sealed class ValidationException : OpenUrzednikException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message, optional variable name and value.
    /// </summary>
    /// <param name="message">Validation message</param>
    /// <param name="ruleName">Rule name</param>
    /// <param name="name">Parameter name</param>
    /// <param name="value">Parameter value</param>
    public ValidationException(string message, string ruleName, string? name, object? value)
        : base(ValidationError.ErrorCode, message)
    {
        ArgumentException.ThrowIfNullOrEmpty(ruleName);

        Data.Add(nameof(ruleName), ruleName);
        if (name is not null)
            Data.Add(nameof(name), name);
        if (value is not null)
            Data.Add(nameof(value), value);
    }
}

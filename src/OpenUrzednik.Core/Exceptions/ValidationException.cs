using System.Globalization;

using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a validation error occurs.
/// When validation found several problems, <see cref="OpenUrzednikException.Errors"/> lists all of them.
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class for several validation errors.
    /// The message lists every error; <see cref="OpenUrzednikException.Errors"/> holds them with their details.
    /// Unlike the single-error constructor, this one doesn't fill <see cref="Exception.Data"/>.
    /// </summary>
    /// <param name="errors">The validation errors, at least one.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errors"/> is empty or contains null.</exception>
    public ValidationException(IReadOnlyList<ValidationError> errors)
        : base(ValidationError.ErrorCode, CreateMessage(errors))
        => SetErrors(errors[0], errors);

    private static string CreateMessage(IReadOnlyList<ValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
            throw new ArgumentException("At least one validation error is required.", nameof(errors));
        if (errors.Any(e => e is null))
            throw new ArgumentException("Validation errors must not contain null.", nameof(errors));

        return errors.Count == 1
            ? errors[0].Message
            : string.Create(CultureInfo.InvariantCulture, $"Validation failed with {errors.Count} errors: {string.Join("; ", errors.Select(e => e.Message))}");
    }
}

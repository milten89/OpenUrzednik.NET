using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Validation;

/// <summary>
/// Base class for a validator that checks one value and reports failures as <see cref="ValidationError"/>s.
/// </summary>
/// <typeparam name="T">Type of the validated value.</typeparam>
public abstract class ValueValidator<T>
{
    /// <summary>Rule name, reported as the error's <c>ruleName</c> metadata (e.g. <c>topCount</c>).</summary>
    public abstract string Name { get; }
    /// <summary>Name of the validated parameter or property, reported as the error's <c>name</c> metadata.</summary>
    public string PropertyName { get; }
    /// <summary>Validated value.</summary>
    public T Value { get; }

    /// <summary>Initializes a validator for one value.</summary>
    /// <param name="propertyName">Name of the validated parameter or property.</param>
    /// <param name="value">Value to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
    protected ValueValidator(string propertyName, T value)
    {
        ArgumentNullException.ThrowIfNull(propertyName, nameof(propertyName));

        PropertyName = propertyName;
        Value = value;
    }

    /// <summary>Validates the value.</summary>
    /// <returns>A successful result, or a failure with a <see cref="ValidationError"/>.</returns>
    public abstract OpenUrzednikResult Validate();

    /// <summary>Creates a failed result with a <see cref="ValidationError"/> for this rule.</summary>
    /// <param name="message">Error message.</param>
    /// <param name="propertyName">Name of the invalid parameter or property.</param>
    /// <param name="value">Invalid value.</param>
    /// <returns>The failed result.</returns>
    protected OpenUrzednikResult GetValidationErrorResult(string message, string? propertyName, object value)
            => OpenUrzednikResult.Failure(new ValidationError(message, Name, propertyName, value));

    /// <summary>Creates a failed result with a <see cref="ValidationError"/> for this rule, <see cref="PropertyName"/> and <see cref="Value"/>.</summary>
    /// <param name="message">Error message.</param>
    /// <returns>The failed result.</returns>
    protected OpenUrzednikResult GetValidationErrorResult(string message)
            => GetValidationErrorResult(message, PropertyName, Value!);
}

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// A <see cref="ValidationError"/> that carries the rejected value with its exact type, e.g. <c>ValidationError&lt;int&gt;</c> for a top
/// count (ADR-0021). The value isn't boxed unless <see cref="OpenUrzednikError.Metadata"/> is read or an exception is created.
/// </summary>
/// <typeparam name="T">The type of the rejected value. Public dates are <see cref="DateTime"/> on every target (ADR-0005).</typeparam>
public sealed class ValidationError<T> : ValidationError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError{T}"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="ruleName">The name of the rule that failed.</param>
    /// <param name="name">The name of the checked parameter or property, when known.</param>
    /// <param name="value">The rejected value.</param>
    public ValidationError(string message, string ruleName, string? name, T value)
        : base(message, ruleName, name, value: null)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the rejected value.
    /// </summary>
    public T Value { get; }

    /// <inheritdoc />
    protected override void CollectMetadata(IDictionary<string, object?> metadata)
    {
        base.CollectMetadata(metadata);
        // A value with no metadata form stays as passed, as with the 1.0 constructor: a ValueValidator<T> of an app's own type kept
        // its value key in 1.0 (ADR-0021). The library's own errors never pass one.
        if (Value is not null)
        {
            var hasForm = MetadataValue.TryConvert(Value, out var converted);
            metadata[ValueMetadataKey] = hasForm ? converted : Value;
        }
    }

    private protected override object? GetExceptionValue() => Value;
}

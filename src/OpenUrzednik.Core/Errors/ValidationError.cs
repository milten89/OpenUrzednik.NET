using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that a validation error has occurred. <see cref="ValidationError{T}"/> carries the rejected value with
/// its exact type, and <see cref="RangeValidationError{T}"/> a rejected range (ADR-0021).
/// </summary>
public class ValidationError : OpenUrzednikError
{
    /// <summary>The <see cref="OpenUrzednikError.Code"/> of this error.</summary>
    public const string ErrorCode = "validation";

    /// <summary>The <see cref="OpenUrzednikError.Metadata"/> key of the name of the rule that failed (always present).</summary>
    public const string RuleNameMetadataKey = "ruleName";

    /// <summary>The <see cref="OpenUrzednikError.Metadata"/> key of the name of the checked parameter or property, when known.</summary>
    public const string NameMetadataKey = "name";

    /// <summary>
    /// The <see cref="OpenUrzednikError.Metadata"/> key of the rejected value, when it isn't <see langword="null"/>: in its metadata form
    /// (e.g. a date as <c>"2026-01-05"</c>), or as passed when it has none.
    /// </summary>
    public const string ValueMetadataKey = "value";

    private readonly object? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError"/> class with a specified error message, variable name and value.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="ruleName">Rule name</param>
    /// <param name="name">Variable name</param>
    /// <param name="value">Variable value</param>
    public ValidationError(string message, string ruleName, string? name, object? value)
        : base(ErrorCode, message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleName);

        RuleName = ruleName;
        Name = name;
        _value = value;
    }

    /// <summary>
    /// Gets the name of the rule that failed (e.g. <c>topCount</c>).
    /// </summary>
    public string RuleName { get; }

    /// <summary>
    /// Gets the name of the checked parameter or property, when known.
    /// </summary>
    public string? Name { get; }

    /// <inheritdoc />
    protected override void CollectMetadata(IDictionary<string, object?> metadata)
    {
        // Stryker disable once Statement : equivalent, OpenUrzednikError.CollectMetadata adds nothing; the call keeps a later base key
        base.CollectMetadata(metadata);
        metadata[RuleNameMetadataKey] = RuleName;
        if (Name is not null)
            metadata[NameMetadataKey] = Name;
        if (_value is not null)
        {
            // A value with no metadata form stays as passed, as in 1.0; the library's own errors never pass one (ADR-0021).
            var hasForm = MetadataValue.TryConvert(_value, out var converted);
            metadata[ValueMetadataKey] = hasForm ? converted : _value;
        }
    }

    /// <inheritdoc />
    protected override OpenUrzednikException CreateException()
        => new OpenUrzednikValidationException(Message, RuleName, Name, GetExceptionValue());

    /// <summary>
    /// Gets the rejected value for <see cref="OpenUrzednikValidationException"/>; boxed only when an exception is created.
    /// </summary>
    private protected virtual object? GetExceptionValue() => _value;
}

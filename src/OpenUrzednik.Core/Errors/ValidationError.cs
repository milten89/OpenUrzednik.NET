using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that a validation error has occurred.
/// </summary>
public sealed class ValidationError : OpenUrzednikError
{
    /// <summary>The <see cref="OpenUrzednikError.Code"/> of this error.</summary>
    public const string ErrorCode = "validation";

    /// <summary>The <see cref="OpenUrzednikError.Metadata"/> key of the name of the rule that failed (always present).</summary>
    public const string RuleNameMetadataKey = "ruleName";

    /// <summary>The <see cref="OpenUrzednikError.Metadata"/> key of the name of the checked parameter or property, when known.</summary>
    public const string NameMetadataKey = "name";

    /// <summary>The <see cref="OpenUrzednikError.Metadata"/> key of the rejected value, when it isn't <see langword="null"/>.</summary>
    public const string ValueMetadataKey = "value";

    private readonly string _ruleName;
    private readonly string? _name;
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

        _ruleName = ruleName;
        _name = name;
        _value = value;

        AddMetadata(RuleNameMetadataKey, ruleName);
        if (name is not null)
            AddMetadata(NameMetadataKey, name);
        if (value is not null)
            AddMetadata(ValueMetadataKey, value);
    }

    /// <inheritdoc />
    protected override OpenUrzednikException CreateException()
        => new OpenUrzednikValidationException(Message, _ruleName, _name, _value);
}

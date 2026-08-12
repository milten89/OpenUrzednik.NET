using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that a validation error has occurred.
/// </summary>
public sealed class ValidationError : OpenUrzednikError
{
    public const string ErrorCode = "validationError";

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

        AddMetadata("ruleName", ruleName);
        if (name is not null)
            AddMetadata("name", name);
        if (value is not null)
            AddMetadata("value", value);
    }

    /// <inheritdoc />
    public override Exception ToException()
        => new ValidationException(Message, _ruleName, _name, _value);
}

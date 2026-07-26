using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that a validation error has occurred.
/// </summary>
public record ValidationError : OpenUrzednikError
{
    private readonly string? _name;
    private readonly object _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError"/> class with a specified error message, variable name and value.
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="name">Variable name</param>
    /// <param name="value">Variable value</param>
    public ValidationError(string message, string? name, object value)
        : base(message)
    {
        _name = name;
        _value = value;

        if (name is not null)
            AddMetadata("name", name);
        AddMetadata("value", value);
    }

    /// <inheritdoc />
    public override Exception ToException()
        => new ValidationException(Message, _name, _value);
}

namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a validation error occurs.
/// </summary>
public class ValidationException : OpenUrzednikException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message, optional variable name and value.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public ValidationException(string message, string? name, object value) 
        : base(message)
    {
        if (name is not null)
            Data.Add("name", name);
        Data.Add("value", value);
    }
}

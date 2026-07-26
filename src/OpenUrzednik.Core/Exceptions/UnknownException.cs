namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when an unknown error occurs.
/// </summary>
public class UnknownException : OpenUrzednikException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public UnknownException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public UnknownException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

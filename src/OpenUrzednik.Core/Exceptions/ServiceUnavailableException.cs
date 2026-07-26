namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when the service is unavailable.
/// </summary>
public class ServiceUnavailableException : OpenUrzednikException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceUnavailableException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ServiceUnavailableException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceUnavailableException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ServiceUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

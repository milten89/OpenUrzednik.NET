using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when the service does not respond within the configured timeout.
/// </summary>
public sealed class RequestTimeoutException : OpenUrzednikException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestTimeoutException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public RequestTimeoutException(string message)
        : base(RequestTimeoutError.ErrorCode, message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestTimeoutException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public RequestTimeoutException(string message, Exception innerException)
        : base(RequestTimeoutError.ErrorCode, message, innerException) { }
}

using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when the service could not be reached or the connection broke.
/// </summary>
public sealed class ConnectionFailedException : OpenUrzednikException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionFailedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConnectionFailedException(string message)
        : base(ConnectionFailedError.ErrorCode, message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionFailedException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConnectionFailedException(string message, Exception innerException)
        : base(ConnectionFailedError.ErrorCode, message, innerException) { }
}

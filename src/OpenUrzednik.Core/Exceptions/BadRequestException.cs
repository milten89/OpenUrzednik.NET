using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when the server rejects the request as invalid.
/// </summary>
public sealed class BadRequestException : OpenUrzednikException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BadRequestException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public BadRequestException(string message)
        : base(BadRequestError.ErrorCode, message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="BadRequestException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public BadRequestException(string message, Exception innerException)
        : base(BadRequestError.ErrorCode, message, innerException) { }
}

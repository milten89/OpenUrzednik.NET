using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when the user is not authorized to perform the requested action.
/// </summary>
public sealed class UnauthorizedException : OpenUrzednikException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public UnauthorizedException(string message)
        : base(UnauthorizedError.ErrorCode, message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public UnauthorizedException(string message, Exception innerException)
        : base(UnauthorizedError.ErrorCode, message, innerException) { }
}

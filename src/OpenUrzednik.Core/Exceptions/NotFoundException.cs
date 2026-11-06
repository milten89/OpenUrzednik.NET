using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a requested resource is not found.
/// </summary>
public sealed class NotFoundException : OpenUrzednikException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public NotFoundException(string message)
        : base(NotFoundError.ErrorCode, message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public NotFoundException(string message, Exception innerException)
        : base(NotFoundError.ErrorCode, message, innerException) { }
}

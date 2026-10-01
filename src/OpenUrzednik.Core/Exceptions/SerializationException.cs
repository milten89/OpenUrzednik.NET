using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a response payload is empty or cannot be deserialized or mapped.
/// </summary>
public sealed class SerializationException : OpenUrzednikException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SerializationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SerializationException(string message)
        : base(SerializationError.ErrorCode, message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SerializationException(string message, Exception innerException)
        : base(SerializationError.ErrorCode, message, innerException) { }
}

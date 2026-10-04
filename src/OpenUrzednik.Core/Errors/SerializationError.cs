using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that a response payload was empty or could not be deserialized or mapped.
/// </summary>
public sealed class SerializationError : OpenUrzednikError
{
    /// <summary>The <see cref="OpenUrzednikError.Code"/> of this error.</summary>
    public const string ErrorCode = "serializationError";

    /// <summary>
    /// Gets the exception that caused the error (for example a <c>System.Text.Json.JsonException</c>), if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializationError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="exception">The exception that caused the error, if any.</param>
    public SerializationError(string message, Exception? exception = null)
        : base(ErrorCode, message)
    {
        Exception = exception;
    }

    /// <inheritdoc />
    /// <remarks>The returned exception keeps <see cref="Exception"/> as its <see cref="System.Exception.InnerException"/>.</remarks>
    protected override OpenUrzednikException CreateException()
        => Exception is null
            ? new OpenUrzednikSerializationException(Message)
            : new OpenUrzednikSerializationException(Message, Exception);
}

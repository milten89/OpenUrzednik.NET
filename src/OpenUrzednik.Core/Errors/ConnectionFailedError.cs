using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the service could not be reached or the connection broke: a DNS, TLS or socket failure
/// while sending the request, or a network failure while reading the response body. No HTTP status is involved.
/// </summary>
/// <param name="message">The message associated with the error.</param>
/// <param name="exception">The exception that caused the error (e.g. an <see cref="System.Net.Http.HttpRequestException"/>), if any.</param>
public sealed class ConnectionFailedError(string message, Exception? exception = null) : OpenUrzednikError(ErrorCode, message)
{
    /// <summary>The <see cref="OpenUrzednikError.Code"/> of this error.</summary>
    public const string ErrorCode = "connectionFailed";

    /// <summary>
    /// Gets the exception that caused the error, if any.
    /// </summary>
    public Exception? Exception { get; } = exception;

    /// <inheritdoc />
    /// <remarks>The returned exception keeps <see cref="Exception"/> as its <see cref="System.Exception.InnerException"/>.</remarks>
    protected override OpenUrzednikException CreateException()
        => Exception is null
            ? new ConnectionFailedException(Message)
            : new ConnectionFailedException(Message, Exception);
}

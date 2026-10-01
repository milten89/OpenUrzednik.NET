using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the service did not respond within the configured timeout.
/// Cancellation requested by the caller is not an error; it throws <see cref="OperationCanceledException"/>.
/// </summary>
/// <param name="message">The message associated with the error.</param>
/// <param name="timeout">The timeout that elapsed, if known.</param>
/// <param name="exception">The exception that signalled the timeout, if any.</param>
public sealed class RequestTimeoutError(string message, TimeSpan? timeout = null, Exception? exception = null) : OpenUrzednikError(ErrorCode, message)
{
    public const string ErrorCode = "requestTimeout";

    /// <summary>
    /// Gets the timeout that elapsed, if known.
    /// </summary>
    public TimeSpan? Timeout { get; } = timeout;

    /// <summary>
    /// Gets the exception that signalled the timeout, if any.
    /// </summary>
    public Exception? Exception { get; } = exception;

    /// <inheritdoc />
    /// <remarks>The returned exception keeps <see cref="Exception"/> as its <see cref="System.Exception.InnerException"/>.</remarks>
    public override Exception ToException()
        => Exception is null
            ? new RequestTimeoutException(Message)
            : new RequestTimeoutException(Message, Exception);
}

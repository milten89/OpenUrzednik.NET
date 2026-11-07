using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the service did not respond within the configured timeout, or answered
/// <c>408 Request Timeout</c> (then <see cref="HttpErrorMetadata.GetHttpStatusCode"/> returns 408).
/// Cancellation requested by the caller is not an error; it throws <see cref="OperationCanceledException"/>.
/// </summary>
public sealed class RequestTimeoutError : OpenUrzednikError
{
    /// <summary>The <see cref="OpenUrzednikError.Code"/> of this error.</summary>
    public const string ErrorCode = "requestTimeout";

    /// <summary>
    /// Gets the timeout that elapsed, if known.
    /// </summary>
    public TimeSpan? Timeout { get; }

    /// <summary>
    /// Gets the exception that signalled the timeout, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestTimeoutError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="timeout">The timeout that elapsed, if known.</param>
    /// <param name="exception">The exception that signalled the timeout, if any.</param>
    /// <param name="statusCode">HTTP status code of the response that caused the error (408), if any; read it with <see cref="HttpErrorMetadata.GetHttpStatusCode"/>.</param>
    public RequestTimeoutError(string message, TimeSpan? timeout = null, Exception? exception = null, int? statusCode = null)
        : base(ErrorCode, message)
    {
        Timeout = timeout;
        Exception = exception;

        if (statusCode.HasValue)
            AddMetadata(HttpErrorMetadata.StatusCodeKey, statusCode.Value);
    }

    /// <inheritdoc />
    /// <remarks>The returned exception keeps <see cref="Exception"/> as its <see cref="System.Exception.InnerException"/>.</remarks>
    protected override OpenUrzednikException CreateException()
    {
        // A statement, not a conditional expression: mutation testing skips mutants inside exception constructor calls (ADR-0018).
        if (Exception is null)
            return new RequestTimeoutException(Message);
        return new RequestTimeoutException(Message, Exception);
    }
}

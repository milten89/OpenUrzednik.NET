using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the service did not respond within the configured timeout, or said that the request timed out,
/// whatever the transport (ADR-0021). An HTTP <c>408 Request Timeout</c> response makes it an <c>HttpRequestTimeoutError</c> (<c>OpenUrzednik.Transport.Http</c>).
/// Cancellation requested by the caller is not an error; it throws <see cref="OperationCanceledException"/>.
/// </summary>
public class RequestTimeoutError : OpenUrzednikError
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
    public RequestTimeoutError(string message)
        : base(ErrorCode, message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestTimeoutError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="timeout">The timeout that elapsed, if known.</param>
    public RequestTimeoutError(string message, TimeSpan? timeout)
        : this(message, timeout, null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestTimeoutError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="timeout">The timeout that elapsed, if known.</param>
    /// <param name="exception">The exception that signalled the timeout, if any.</param>
    public RequestTimeoutError(string message, TimeSpan? timeout, Exception? exception)
        : base(ErrorCode, message)
    {
        Timeout = timeout;
        Exception = exception;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestTimeoutError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="timeout">The timeout that elapsed, if known.</param>
    /// <param name="exception">The exception that signalled the timeout, if any.</param>
    /// <param name="statusCode">HTTP status code of the response that caused the error (408), if any; read it with <see cref="HttpErrorMetadata.GetHttpStatusCode"/>.</param>
    [Obsolete("A condition type takes no transport details: use a RequestTimeoutError constructor without statusCode (for an exception only: new RequestTimeoutError(message, null, exception)), or HttpRequestTimeoutError (OpenUrzednik.Transport.Http) for an error caused by an HTTP response (ADR-0021). Removed in 2.0.")]
    public RequestTimeoutError(string message, TimeSpan? timeout = null, Exception? exception = null, int? statusCode = null)
        : this(message, timeout, exception)
    {
        if (statusCode.HasValue)
            AddMetadata(HttpErrorMetadata.StatusCodeKey, statusCode.Value);
    }

    /// <inheritdoc />
    /// <remarks>The returned exception keeps <see cref="Exception"/> as its <see cref="System.Exception.InnerException"/>.</remarks>
    protected override OpenUrzednikException CreateException()
    {
        // A statement, not a conditional expression: mutation testing skips mutants inside exception constructor calls (ADR-0020).
        if (Exception is null)
            return new RequestTimeoutException(Message);
        return new RequestTimeoutException(Message, Exception);
    }
}

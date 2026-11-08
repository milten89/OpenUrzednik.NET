using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the service is currently unavailable, whatever the transport (ADR-0022): it answered with a
/// server error (an HTTP 5xx makes it an <c>HttpServiceUnavailableError</c> (<c>OpenUrzednik.Transport.Http</c>)), or a resilience handler's circuit breaker or
/// rate limiter rejected the request. A network failure is a <see cref="ConnectionFailedError"/>.
/// </summary>
public class ServiceUnavailableError : OpenUrzednikError
{
    /// <summary>The <see cref="OpenUrzednikError.Code"/> of this error.</summary>
    public const string ErrorCode = "serviceUnavailable";

    /// <summary>
    /// Gets the exception that caused the error, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceUnavailableError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    public ServiceUnavailableError(string message)
        : base(ErrorCode, message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceUnavailableError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="exception">The exception that caused the error (e.g. the resilience handler's rejection), if any.</param>
    public ServiceUnavailableError(string message, Exception? exception)
        : base(ErrorCode, message)
    {
        Exception = exception;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceUnavailableError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="statusCode">HTTP status code of the response that caused the error, if any; read it with <see cref="HttpErrorMetadata.GetHttpStatusCode"/>.</param>
    /// <param name="exception">The exception that caused the error (e.g. the resilience handler's rejection), if any.</param>
    [Obsolete("A condition type takes no transport details: use a ServiceUnavailableError constructor without statusCode, or HttpServiceUnavailableError (OpenUrzednik.Transport.Http) for an error caused by an HTTP response (ADR-0022). Removed in 2.0.")]
    public ServiceUnavailableError(string message, int? statusCode = null, Exception? exception = null)
        : this(message, exception)
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
            return new ServiceUnavailableException(Message);
        return new ServiceUnavailableException(Message, Exception);
    }
}

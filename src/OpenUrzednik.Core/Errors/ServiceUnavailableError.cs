using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the service is currently unavailable: it returned a 5xx status
/// (<see cref="HttpErrorMetadata.GetHttpStatusCode"/> returns it), or a resilience handler's circuit breaker or rate limiter
/// rejected the request (no status). A network failure is a <see cref="ConnectionFailedError"/>.
/// </summary>
public sealed class ServiceUnavailableError : OpenUrzednikError
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
    /// <param name="statusCode">HTTP status code of the response that caused the error, if any; read it with <see cref="HttpErrorMetadata.GetHttpStatusCode"/>.</param>
    /// <param name="exception">The exception that caused the error (e.g. the resilience handler's rejection), if any.</param>
    public ServiceUnavailableError(string message, int? statusCode = null, Exception? exception = null)
        : base(ErrorCode, message)
    {
        Exception = exception;

        if (statusCode.HasValue)
            AddMetadata(HttpErrorMetadata.StatusCodeKey, statusCode.Value);
    }

    /// <inheritdoc />
    /// <remarks>The returned exception keeps <see cref="Exception"/> as its <see cref="System.Exception.InnerException"/>.</remarks>
    protected override OpenUrzednikException CreateException()
        => Exception is null
            ? new ServiceUnavailableException(Message)
            : new ServiceUnavailableException(Message, Exception);
}

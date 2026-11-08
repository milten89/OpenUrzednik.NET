using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Transport.Http;

/// <summary>
/// A <see cref="RateLimitExceededError"/> caused by an HTTP response: the server answered <c>429 Too Many Requests</c>, or a provider mapped another status to it (ADR-0021).
/// </summary>
/// <remarks>
/// It keeps the <see cref="OpenUrzednikError.Code"/> of <see cref="RateLimitExceededError"/>, so code that checks <c>is RateLimitExceededError</c> or the code handles it
/// like any other. Read the status from <see cref="StatusCode"/>, through <see cref="IOpenUrzednikHttpStatusCode"/>, or with
/// <see cref="HttpErrorMetadata.GetHttpStatusCode"/>, which reads it from <see cref="OpenUrzednikError.Metadata"/>
/// (<see cref="HttpErrorMetadata.StatusCodeKey"/>) in 1.x (ADR-0021).
/// </remarks>
public sealed class HttpRateLimitExceededError : RateLimitExceededError, IOpenUrzednikHttpStatusCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRateLimitExceededError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="retryAfter">The delay from the response's <c>Retry-After</c> header, if it has one.</param>
    /// <param name="statusCode">The status code of the HTTP response that caused the error.</param>
    public HttpRateLimitExceededError(string message, TimeSpan? retryAfter, int statusCode)
        : base(message, retryAfter)
    {
        StatusCode = statusCode;
        AddMetadata(HttpErrorMetadata.StatusCodeKey, statusCode);
    }

    /// <inheritdoc />
    public int StatusCode { get; }
}

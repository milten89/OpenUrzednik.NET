using System.Net;

using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Transport.Http;

/// <summary>
/// A <see cref="RateLimitExceededError"/> caused by an HTTP response: the server answered <c>429 Too Many Requests</c>, or a provider mapped another status to it (ADR-0021).
/// </summary>
/// <remarks>
/// It keeps the <see cref="OpenUrzednikError.Code"/> and exception of <see cref="RateLimitExceededError"/>, so code that checks <c>is RateLimitExceededError</c> or the
/// code handles it like any other. Read the status from <see cref="StatusCode"/>, through <see cref="IOpenUrzednikHttpStatusCode"/>, or
/// with <see cref="HttpStatusExtensions.GetHttpStatus"/>; <see cref="OpenUrzednikError.Metadata"/> holds it as an <see cref="int"/> under
/// <see cref="HttpStatusExtensions.StatusCodeMetadataKey"/>.
/// It isn't sealed: a provider derives its own error type from it to add typed details of its API; that error is still this type and
/// its condition (ADR-0021).
/// </remarks>
public class HttpRateLimitExceededError : RateLimitExceededError, IOpenUrzednikHttpStatusCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRateLimitExceededError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="retryAfter">The delay from the response's <c>Retry-After</c> header, if it has one.</param>
    /// <param name="statusCode">The status code of the HTTP response that caused the error.</param>
    public HttpRateLimitExceededError(string message, TimeSpan? retryAfter, HttpStatusCode statusCode)
        : base(message, retryAfter)
    {
        StatusCode = statusCode;
    }

    /// <inheritdoc />
    public HttpStatusCode StatusCode { get; }

    /// <inheritdoc />
    protected override void CollectMetadata(IDictionary<string, object?> metadata)
    {
        base.CollectMetadata(metadata);
        metadata[HttpStatusExtensions.StatusCodeMetadataKey] = (int)StatusCode;
    }
}

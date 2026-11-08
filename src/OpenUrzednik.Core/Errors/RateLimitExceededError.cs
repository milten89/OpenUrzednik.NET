using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the rate limit has been exceeded, whatever the transport (ADR-0021).
/// An HTTP response that caused it makes it an <c>HttpRateLimitExceededError</c> (<c>OpenUrzednik.Transport.Http</c>).
/// </summary>
public class RateLimitExceededError : OpenUrzednikError
{
    /// <summary>The <see cref="OpenUrzednikError.Code"/> of this error.</summary>
    public const string ErrorCode = "rateLimitExceeded";

    /// <summary>The <see cref="OpenUrzednikError.Metadata"/> key of <see cref="RetryAfter"/>.</summary>
    public const string RetryAfterMetadataKey = "retryAfter";

    /// <summary>
    /// How long to wait before retrying; <see langword="null"/> when the service didn't say. From HTTP, it is the response's
    /// <c>Retry-After</c> header: a date in the header is converted to a delay from the response's <c>Date</c> header (or now),
    /// so a past date gives a negative value.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="retryAfter">How long to wait before retrying, if the service said so.</param>
    public RateLimitExceededError(string message, TimeSpan? retryAfter)
        : base(ErrorCode, message)
    {
        RetryAfter = retryAfter;
        AddMetadata(RetryAfterMetadataKey, retryAfter);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="retryAfter">How long to wait before retrying, if the server said so.</param>
    /// <param name="statusCode">HTTP status code of the response that caused the error, if any; read it with <see cref="HttpErrorMetadata.GetHttpStatusCode"/>.</param>
    [Obsolete("A condition type takes no transport details: use RateLimitExceededError(string, TimeSpan?), or HttpRateLimitExceededError (OpenUrzednik.Transport.Http) for an error caused by an HTTP response (ADR-0021). Removed in 2.0.")]
    public RateLimitExceededError(string message, TimeSpan? retryAfter, int? statusCode = null)
        : this(message, retryAfter)
    {
        if (statusCode.HasValue)
            AddMetadata(HttpErrorMetadata.StatusCodeKey, statusCode.Value);
    }

    /// <inheritdoc />
    protected override OpenUrzednikException CreateException()
        => new RateLimitExceededException(Message, RetryAfter);
}

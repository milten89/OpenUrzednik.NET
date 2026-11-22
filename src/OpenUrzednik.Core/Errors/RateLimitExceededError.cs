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

    /// <summary>
    /// The <see cref="OpenUrzednikError.Metadata"/> key of <see cref="RetryAfter"/> as a <see cref="TimeSpan"/> (always present;
    /// <see langword="null"/> when the service didn't say). Kept from 1.0 and still written in 1.x; removed in 2.0, with the key itself:
    /// read <see cref="RetryAfterDurationMetadataKey"/> or <see cref="RetryAfter"/> instead (ADR-0021).
    /// </summary>
    [Obsolete("The retryAfter metadata key holds a TimeSpan, the one value outside the JSON-shaped metadata rule: read RetryAfterDurationMetadataKey (an ISO 8601 duration) or the RetryAfter property instead (ADR-0021). Removed in 2.0.")]
    public const string RetryAfterMetadataKey = "retryAfter";

    /// <summary>
    /// The <see cref="OpenUrzednikError.Metadata"/> key of <see cref="RetryAfter"/> as an ISO 8601 duration (e.g. <c>"PT30S"</c>; always
    /// present, <see langword="null"/> when the service didn't say).
    /// </summary>
    public const string RetryAfterDurationMetadataKey = "retryAfterDuration";

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
    protected override void CollectMetadata(IDictionary<string, object?> metadata)
    {
        // Stryker disable once Statement : equivalent, OpenUrzednikError.CollectMetadata adds nothing; the call keeps a later base key
        base.CollectMetadata(metadata);
        var retryAfter = RetryAfter;
#pragma warning disable CS0618 // the 1.0 key is still written until 2.0 removes it (ADR-0021)
        metadata[RetryAfterMetadataKey] = retryAfter;
#pragma warning restore CS0618
        metadata[RetryAfterDurationMetadataKey] = retryAfter.HasValue ? MetadataValue.FormatDuration(retryAfter.Value) : null;
    }

    /// <inheritdoc />
    protected override OpenUrzednikException CreateException()
        => new RateLimitExceededException(Message, RetryAfter);
}

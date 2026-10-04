using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the rate limit has been exceeded.
/// </summary>
public sealed class RateLimitExceededError : OpenUrzednikError
{
    /// <summary>The <see cref="OpenUrzednikError.Code"/> of this error.</summary>
    public const string ErrorCode = "rateLimitExceeded";

    /// <summary>
    /// How long to wait before retrying, from the response's <c>Retry-After</c> header; <see langword="null"/> when the server didn't say.
    /// A date in the header is converted to a delay from the response's <c>Date</c> header (or now), so a past date gives a negative value.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="retryAfter">How long to wait before retrying, if the server said so.</param>
    /// <param name="statusCode">HTTP status code of the response that caused the error, if any.</param>
    public RateLimitExceededError(string message, TimeSpan? retryAfter, int? statusCode = null)
        : base(ErrorCode, message, statusCode)
    {
        RetryAfter = retryAfter;

        AddMetadata("retryAfter", retryAfter);
    }

    /// <inheritdoc />
    protected override OpenUrzednikException CreateException()
        => new RateLimitExceededException(Message, RetryAfter);
}

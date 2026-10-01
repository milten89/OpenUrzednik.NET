using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the rate limit has been exceeded.
/// </summary>
public sealed class RateLimitExceededError : OpenUrzednikError
{
    public const string ErrorCode = "rateLimitExceeded";

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
    public override Exception ToException()
        => new RateLimitExceededException(Message, RetryAfter);
}

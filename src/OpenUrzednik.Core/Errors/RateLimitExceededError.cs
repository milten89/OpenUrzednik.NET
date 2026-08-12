using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the rate limit has been exceeded.
/// </summary>
public sealed class RateLimitExceededError : OpenUrzednikError
{
    public const string ErrorCode = "rateLimitExceeded";

    public TimeSpan? RetryAfter { get; }

    public RateLimitExceededError(string message, TimeSpan? retryAfter)
        : base(ErrorCode, message)
    {
        RetryAfter = retryAfter;

        AddMetadata("retryAfter", retryAfter);
    }

    /// <inheritdoc />
    public override Exception ToException()
        => new RateLimitExceededException(Message, RetryAfter);
}

using OpenUrzednik.Core.Exceptions;

namespace OpenUrzednik.Core.Errors;

/// <summary>
/// Represents an error indicating that the rate limit has been exceeded.
/// </summary>
/// <param name="Message">The message associated with the error.</param>
/// <param name="RetryAfter">Delay between next request</param>
public record RateLimitExceededError(string Message, TimeSpan? RetryAfter) : OpenUrzednikError(Message)
{
    /// <inheritdoc />
    public override Exception ToException()
        => new RateLimitExceededException(Message, RetryAfter);
}

namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when the rate limit has been exceeded.
/// </summary>
public class RateLimitExceededException : OpenUrzednikException
{
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="retryAfter">Delay between next request</param>
    public RateLimitExceededException(string message, TimeSpan? retryAfter) : base(message)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="retryAfter">Delay between next request</param>
    /// <param name="innerException">The inner exception.</param>
    public RateLimitExceededException(string message, TimeSpan? retryAfter, Exception innerException) : base(message, innerException)
    {
        RetryAfter = retryAfter;
    }
}

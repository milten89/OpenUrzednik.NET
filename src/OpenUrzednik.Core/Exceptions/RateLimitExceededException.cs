namespace OpenUrzednik.Core.Exceptions;

/// <summary>
/// Represents an exception that is thrown when the rate limit has been exceeded.
/// </summary>
public class RateLimitExceededException : OpenUrzednikException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public RateLimitExceededException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitExceededException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public RateLimitExceededException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

namespace OpenUrzednik.Http.Infrastructure;

/// <summary>
/// Marks a request that a resilience handler rejected without sending it (an open circuit breaker or a rate limiter), so the executor
/// returns a <c>ServiceUnavailableError</c> instead of a <c>ConnectionFailedError</c> (ADR-0002, ADR-0004).
/// The DI package compiles this file too and throws <see cref="Create"/>; the marker is an <see cref="Exception.Data"/> entry, because a
/// marker type would differ per assembly unless it were public.
/// </summary>
internal static class ResilienceRejection
{
    private const string DataKey = "OpenUrzednik.ResilienceRejected";

    /// <summary>
    /// Creates the <see cref="HttpRequestException"/> (the type <see cref="HttpClient"/> lets through from handlers) for a rejection.
    /// </summary>
    internal static HttpRequestException Create(string message, Exception rejection)
    {
        var exception = new HttpRequestException(message, rejection);
        exception.Data[DataKey] = true;
        return exception;
    }

    /// <summary>
    /// Whether <paramref name="exception"/> is, or wraps one level down, an exception from <see cref="Create"/>.
    /// .NET Framework's <see cref="HttpClient"/> may wrap what a handler throws.
    /// </summary>
    internal static bool IsRejection(Exception exception)
        => exception.Data.Contains(DataKey) || exception.InnerException?.Data.Contains(DataKey) == true;
}

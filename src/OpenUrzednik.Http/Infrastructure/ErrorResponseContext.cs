using System.Net;

namespace OpenUrzednik.Http.Infrastructure;

/// <summary>
/// A non-success response passed to <see cref="RestProviderProfile.MapErrorAsync"/>. For provider authors.
/// It is valid only until the override's task completes: the response is disposed afterwards.
/// </summary>
public sealed class ErrorResponseContext
{
    private readonly Func<Task<string?>> _readMessageAsync;

    internal ErrorResponseContext(HttpResponseMessage response, string requestPath, TimeSpan? retryAfter, Func<Task<string?>> readMessageAsync, CancellationToken cancellationToken)
    {
        Response = response;
        RequestPath = requestPath;
        RetryAfter = retryAfter;
        _readMessageAsync = readMessageAsync;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the HTTP status code of the response.
    /// </summary>
    public HttpStatusCode StatusCode => Response.StatusCode;

    /// <summary>
    /// Gets the response. Its body hasn't been read; prefer <see cref="ReadMessageAsync"/>, which reads it safely.
    /// </summary>
    public HttpResponseMessage Response { get; }

    /// <summary>
    /// Gets the requested URI, for messages.
    /// </summary>
    public string RequestPath { get; }

    /// <summary>
    /// Gets the delay from the <c>Retry-After</c> header, if any.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Gets the token for reading the body. It is cancelled by the caller or when the request deadline elapses.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Reads at most the first 500 characters of the body as text and trims them.
    /// Returns <see langword="null"/> when there is no body or it can't be read before the deadline.
    /// Throws <see cref="OperationCanceledException"/> only when the caller cancelled.
    /// </summary>
    /// <returns>The body text, or <see langword="null"/>.</returns>
    public Task<string?> ReadMessageAsync() => _readMessageAsync();
}

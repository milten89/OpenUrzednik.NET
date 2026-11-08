namespace OpenUrzednik.Core.Errors;

/// <summary>
/// The HTTP details an error can carry in its <see cref="OpenUrzednikError.Metadata"/>, with typed accessors.
/// </summary>
/// <remarks>
/// Error types describe the condition (not found, rate limited, unavailable), whatever the transport (ADR-0012), so the
/// status code isn't a property of a type: the same <see cref="NotFoundError"/> comes from a 404, from a response with no
/// data, or from a provider that doesn't use HTTP. Errors caused by an HTTP response store its status code here.
/// </remarks>
public static class HttpErrorMetadata
{
    /// <summary>
    /// The <see cref="OpenUrzednikError.Metadata"/> key of the status code (an <see cref="int"/>) of the HTTP response that caused the error.
    /// </summary>
    public const string StatusCodeKey = "statusCode";

    /// <summary>
    /// Gets the status code of the HTTP response that caused the error.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <param name="statusCode">The status code, or 0 when the error has none.</param>
    /// <returns><see langword="true"/> when an HTTP response caused the error; otherwise <see langword="false"/> (e.g. a validation
    /// error, a network failure, a client-side timeout or a resilience handler's rejection).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static bool TryGetHttpStatusCode(this OpenUrzednikError error, out int statusCode)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (error.Metadata.TryGetValue(StatusCodeKey, out var value) && value is int code)
        {
            statusCode = code;
            return true;
        }

        statusCode = 0;
        return false;
    }

    /// <summary>
    /// Gets the status code of the HTTP response that caused the error, or <see langword="null"/> when no response caused it.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The status code, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static int? GetHttpStatusCode(this OpenUrzednikError error)
    {
        // A local, not the call as the condition, so that mutation testing checks the conditional (ADR-0020): replacing the
        // condition with `true` or `false` would remove the `out var` declaration, so those mutants couldn't compile.
        var hasStatusCode = error.TryGetHttpStatusCode(out var statusCode);
        return hasStatusCode ? statusCode : null;
    }
}

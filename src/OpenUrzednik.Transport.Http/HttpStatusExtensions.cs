using System.Net;

using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Transport.Http;

/// <summary>
/// Reads the HTTP status code of any error without a type check (ADR-0021).
/// </summary>
public static class HttpStatusExtensions
{
    /// <summary>
    /// The <see cref="OpenUrzednikError.Metadata"/> key of the status code of the HTTP response that caused the error, an <see cref="int"/>
    /// (as OpenTelemetry's <c>http.response.status_code</c>). The same key as the obsolete <c>HttpErrorMetadata.StatusCodeKey</c> in Core.
    /// </summary>
    public const string StatusCodeMetadataKey = "statusCode";

    /// <summary>
    /// Gets the status code of the HTTP response that caused the error.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <param name="statusCode">The status code, or 0 when the error has none.</param>
    /// <returns><see langword="true"/> when an HTTP response caused the error; otherwise <see langword="false"/> (e.g. a validation
    /// error, a network failure, a client-side timeout or a resilience handler's rejection).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static bool TryGetHttpStatus(this OpenUrzednikError error, out HttpStatusCode statusCode)
    {
        ArgumentNullException.ThrowIfNull(error);

        // A local with `as`, not a pattern variable, so that mutation testing can compile the negated condition (ADR-0020).
        var httpError = error as IOpenUrzednikHttpStatusCode;
        if (httpError is not null)
        {
            statusCode = httpError.StatusCode;
            return true;
        }

        // Errors built with Core's obsolete statusCode constructors carry the status only in the metadata; removed in 2.0 with them.
        // Statements, not `out var` and a pattern in the condition, so that mutation testing can compile its mutants (ADR-0020).
        _ = error.Metadata.TryGetValue(StatusCodeMetadataKey, out var value);
        var code = value as int?;
        if (code is not null)
        {
            statusCode = (HttpStatusCode)code.Value;
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
    public static HttpStatusCode? GetHttpStatus(this OpenUrzednikError error)
    {
        // A local, not the call as the condition, so that mutation testing checks the conditional (ADR-0020).
        var hasStatusCode = error.TryGetHttpStatus(out var statusCode);
        return hasStatusCode ? statusCode : null;
    }
}

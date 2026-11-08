using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Transport.Http;

/// <summary>
/// A <see cref="RequestTimeoutError"/> caused by an HTTP response: the server answered <c>408 Request Timeout</c>, or a provider mapped another status to it (ADR-0021).
/// </summary>
/// <remarks>
/// It keeps the <see cref="OpenUrzednikError.Code"/> of <see cref="RequestTimeoutError"/>, so code that checks <c>is RequestTimeoutError</c> or the code handles it
/// like any other. Read the status from <see cref="StatusCode"/>, through <see cref="IOpenUrzednikHttpStatusCode"/>, or with
/// <see cref="HttpErrorMetadata.GetHttpStatusCode"/>, which reads it from <see cref="OpenUrzednikError.Metadata"/>
/// (<see cref="HttpErrorMetadata.StatusCodeKey"/>) in 1.x (ADR-0021).
/// </remarks>
public sealed class HttpRequestTimeoutError : RequestTimeoutError, IOpenUrzednikHttpStatusCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRequestTimeoutError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="statusCode">The status code of the HTTP response that caused the error.</param>
    public HttpRequestTimeoutError(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
        AddMetadata(HttpErrorMetadata.StatusCodeKey, statusCode);
    }

    /// <inheritdoc />
    public int StatusCode { get; }
}

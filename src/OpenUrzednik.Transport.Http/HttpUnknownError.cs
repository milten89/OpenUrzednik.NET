using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Transport.Http;

/// <summary>
/// An <see cref="UnknownError"/> caused by an HTTP response: the server answered with a status no other error describes, or a provider mapped another status to it (ADR-0021).
/// </summary>
/// <remarks>
/// It keeps the <see cref="OpenUrzednikError.Code"/> of <see cref="UnknownError"/>, so code that checks <c>is UnknownError</c> or the code handles it
/// like any other. Read the status from <see cref="StatusCode"/>, through <see cref="IOpenUrzednikHttpStatusCode"/>, or with
/// <see cref="HttpErrorMetadata.GetHttpStatusCode"/>, which reads it from <see cref="OpenUrzednikError.Metadata"/>
/// (<see cref="HttpErrorMetadata.StatusCodeKey"/>) in 1.x (ADR-0021).
/// </remarks>
public sealed class HttpUnknownError : UnknownError, IOpenUrzednikHttpStatusCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpUnknownError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="statusCode">The status code of the HTTP response that caused the error.</param>
    public HttpUnknownError(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
        AddMetadata(HttpErrorMetadata.StatusCodeKey, statusCode);
    }

    /// <inheritdoc />
    public int StatusCode { get; }
}

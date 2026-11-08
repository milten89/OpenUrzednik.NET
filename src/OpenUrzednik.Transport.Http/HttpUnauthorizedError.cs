using System.Net;

using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Transport.Http;

/// <summary>
/// An <see cref="UnauthorizedError"/> caused by an HTTP response: the server answered <c>401 Unauthorized</c> or <c>403 Forbidden</c>, or a provider mapped another status to it (ADR-0022).
/// </summary>
/// <remarks>
/// It keeps the <see cref="OpenUrzednikError.Code"/> and exception of <see cref="UnauthorizedError"/>, so code that checks <c>is UnauthorizedError</c> or the
/// code handles it like any other. Read the status from <see cref="StatusCode"/>, through <see cref="IOpenUrzednikHttpStatusCode"/>, or
/// with <see cref="HttpStatusExtensions.GetHttpStatus"/>; <see cref="OpenUrzednikError.Metadata"/> holds it as an <see cref="int"/> under
/// <see cref="HttpStatusExtensions.StatusCodeMetadataKey"/>.
/// It isn't sealed: a provider derives its own error type from it to add typed details of its API; that error is still this type and
/// its condition (ADR-0022).
/// </remarks>
public class HttpUnauthorizedError : UnauthorizedError, IOpenUrzednikHttpStatusCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpUnauthorizedError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="statusCode">The status code of the HTTP response that caused the error.</param>
    public HttpUnauthorizedError(string message, HttpStatusCode statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <inheritdoc />
    public HttpStatusCode StatusCode { get; }

    /// <inheritdoc />
    protected override void CollectMetadata(IDictionary<string, object?> metadata)
    {
        // Stryker disable once Statement : equivalent, UnauthorizedError adds no metadata of its own today; the call keeps a later condition key
        base.CollectMetadata(metadata);
        metadata[HttpStatusExtensions.StatusCodeMetadataKey] = (int)StatusCode;
    }
}

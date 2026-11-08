using System.Net;

using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.Transport.Http;

/// <summary>
/// A <see cref="ServiceUnavailableError"/> caused by an HTTP response: the server answered with a 5xx status, or a provider mapped another status to it (ADR-0021).
/// </summary>
/// <remarks>
/// It keeps the <see cref="OpenUrzednikError.Code"/> and exception of <see cref="ServiceUnavailableError"/>, so code that checks <c>is ServiceUnavailableError</c> or the
/// code handles it like any other. Read the status from <see cref="StatusCode"/>, through <see cref="IOpenUrzednikHttpStatusCode"/>, or
/// with <see cref="HttpStatusExtensions.GetHttpStatus"/>; <see cref="OpenUrzednikError.Metadata"/> holds it as an <see cref="int"/> under
/// <see cref="HttpStatusExtensions.StatusCodeMetadataKey"/>.
/// </remarks>
public sealed class HttpServiceUnavailableError : ServiceUnavailableError, IOpenUrzednikHttpStatusCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServiceUnavailableError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="statusCode">The status code of the HTTP response that caused the error.</param>
    public HttpServiceUnavailableError(string message, HttpStatusCode statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <inheritdoc />
    public HttpStatusCode StatusCode { get; }

    /// <inheritdoc />
    protected override void CollectMetadata(IDictionary<string, object?> metadata)
    {
        // Stryker disable once Statement : equivalent, ServiceUnavailableError adds no metadata of its own today; the call keeps a later condition key
        base.CollectMetadata(metadata);
        metadata[HttpStatusExtensions.StatusCodeMetadataKey] = (int)StatusCode;
    }
}

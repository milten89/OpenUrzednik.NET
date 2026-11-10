using System.Net;

using OpenUrzednik.Transport.Http;

namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// The MF White List API rejected the request with <c>400 Bad Request</c> (ADR-0023): a parameter it doesn't accept (e.g. <c>WL-115</c>,
/// an invalid NIP; <c>WL-118</c>, a date before the list's range), or a 400 without a code. It is an <see cref="HttpBadRequestError"/>,
/// so <c>is BadRequestError</c> and the status code work as for any provider, and it carries the API's code.
/// </summary>
/// <remarks>
/// The clients check their arguments before sending, so this error means the API rejected something they accepted. The 400s with
/// <c>WL-195</c> and <c>WL-196</c> ask to repeat the query: they are a <see cref="WhiteListServiceUnavailableError"/>. A rejection
/// inside a successful answer is a <see cref="WhiteListQueryRejectedError"/>; <c>is BadRequestError</c> covers both.
/// </remarks>
public sealed class WhiteListBadRequestError : HttpBadRequestError, IWhiteListErrorCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhiteListBadRequestError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error, including the server's message (identifiers masked) if it had one.</param>
    /// <param name="statusCode">The status code of the HTTP response that caused the error.</param>
    /// <param name="whiteListCode">The API's error code, or <see langword="null"/> when the answer had none.</param>
    public WhiteListBadRequestError(string message, HttpStatusCode statusCode, string? whiteListCode)
        : base(message, statusCode)
    {
        WhiteListCode = whiteListCode;
    }

    /// <inheritdoc />
    public string? WhiteListCode { get; }

    /// <inheritdoc />
    protected override void CollectMetadata(IDictionary<string, object?> metadata)
    {
        base.CollectMetadata(metadata);
        metadata[WhiteListErrorMetadata.WhiteListCodeMetadataKey] = WhiteListCode;
    }
}

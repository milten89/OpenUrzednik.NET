using System.Net;

using OpenUrzednik.Transport.Http;

namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// The MF White List API can't answer now (ADR-0023): a 5xx status (e.g. <c>WL-100</c>, an internal error), or a 400 with
/// <c>WL-195</c> ("the database was updated, repeat the query") or <c>WL-196</c> ("update in progress, try later"), which the ministry
/// returns while it updates the list, mostly between 22:00 and 24:00 on working days. It is an <see cref="HttpServiceUnavailableError"/>,
/// so <c>is ServiceUnavailableError</c> and the status code work as for any provider, and it carries the API's code.
/// </summary>
/// <remarks>A retry after a short delay usually succeeds.</remarks>
public sealed class WhiteListServiceUnavailableError : HttpServiceUnavailableError, IWhiteListErrorCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhiteListServiceUnavailableError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="statusCode">The status code of the HTTP response that caused the error.</param>
    /// <param name="whiteListCode">The API's error code, or <see langword="null"/> when the answer had none.</param>
    public WhiteListServiceUnavailableError(string message, HttpStatusCode statusCode, string? whiteListCode)
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

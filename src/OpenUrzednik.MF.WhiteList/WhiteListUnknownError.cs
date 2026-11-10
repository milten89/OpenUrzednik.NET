using System.Net;

using OpenUrzednik.Transport.Http;

namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// The MF White List API answered <c>500</c> with <c>WL-197</c>: the system can't determine the entity's historical status for the date
/// (ADR-0023). It is an <see cref="HttpUnknownError"/>, so <c>is UnknownError</c> and the status code work as for any provider, and it
/// carries the API's code.
/// </summary>
/// <remarks>A retry can't fix it and would spend quota.</remarks>
public sealed class WhiteListUnknownError : HttpUnknownError, IWhiteListErrorCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhiteListUnknownError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="statusCode">The status code of the HTTP response that caused the error.</param>
    /// <param name="whiteListCode">The API's error code, or <see langword="null"/> when the answer had none.</param>
    public WhiteListUnknownError(string message, HttpStatusCode statusCode, string? whiteListCode)
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

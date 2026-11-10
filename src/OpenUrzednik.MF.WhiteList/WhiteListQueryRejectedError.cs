using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// The MF White List API rejected a query inside a successful answer (ADR-0023): a batch entry with an <c>error</c>, or WL2's
/// <c>I</c> (invalid NIP) or <c>D</c> (date out of range). No HTTP status caused it, so it derives from <see cref="BadRequestError"/>
/// directly; <c>is BadRequestError</c> covers it and the HTTP 400 <see cref="WhiteListBadRequestError"/> alike.
/// </summary>
public sealed class WhiteListQueryRejectedError : BadRequestError, IWhiteListErrorCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhiteListQueryRejectedError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="whiteListCode">The API's code of the rejection, or <see langword="null"/> when the answer had none.</param>
    public WhiteListQueryRejectedError(string message, string? whiteListCode)
        : base(message)
    {
        WhiteListCode = whiteListCode;
    }

    /// <inheritdoc />
    public string? WhiteListCode { get; }

    /// <inheritdoc />
    protected override void CollectMetadata(IDictionary<string, object?> metadata)
    {
        // Stryker disable once Statement : equivalent, BadRequestError adds no metadata of its own today; the call keeps a later condition key
        base.CollectMetadata(metadata);
        metadata[WhiteListErrorMetadata.WhiteListCodeMetadataKey] = WhiteListCode;
    }
}

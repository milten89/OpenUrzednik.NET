using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// The MF White List status service (WL2) answered with <c>X</c>, "service inactive", inside a successful answer (ADR-0023). No HTTP status
/// caused it, so it derives from <see cref="ServiceUnavailableError"/> directly; <c>is ServiceUnavailableError</c> covers it.
/// </summary>
public sealed class WhiteListServiceInactiveError : ServiceUnavailableError, IWhiteListErrorCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhiteListServiceInactiveError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="whiteListCode">The API's code (<c>X</c>), or <see langword="null"/> when the answer had none.</param>
    public WhiteListServiceInactiveError(string message, string? whiteListCode)
        : base(message)
    {
        WhiteListCode = whiteListCode;
    }

    /// <inheritdoc />
    public string? WhiteListCode { get; }

    /// <inheritdoc />
    protected override void CollectMetadata(IDictionary<string, object?> metadata)
    {
        // Stryker disable once Statement : equivalent, ServiceUnavailableError adds no metadata of its own today; the call keeps a later condition key
        base.CollectMetadata(metadata);
        metadata[WhiteListErrorMetadata.WhiteListCodeMetadataKey] = WhiteListCode;
    }
}

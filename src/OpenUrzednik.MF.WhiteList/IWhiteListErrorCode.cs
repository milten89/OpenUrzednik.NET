namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// The MF White List API's own error code, carried by every MF White List error type a code can reach (ADR-0021, ADR-0023):
/// <see cref="WhiteListRateLimitExceededError"/>, <see cref="WhiteListServiceUnavailableError"/>, <see cref="WhiteListBadRequestError"/>,
/// <see cref="WhiteListUnknownError"/>, <see cref="WhiteListQueryRejectedError"/> and <see cref="WhiteListServiceInactiveError"/>.
/// One <c>is IWhiteListErrorCode</c> check reads it whatever the condition.
/// </summary>
/// <remarks>
/// <see cref="OpenUrzednik.Core.Errors.OpenUrzednikError.Metadata"/> holds the code under
/// <see cref="WhiteListErrorMetadata.WhiteListCodeMetadataKey"/>. This interface is frozen: a further detail comes as a separate interface,
/// never as a new member (ADR-0011).
/// </remarks>
public interface IWhiteListErrorCode
{
    /// <summary>
    /// Gets the API's code, e.g. <c>WL-191</c> (the daily limit), <c>WL-195</c> (repeat the query), <c>WL-115</c> (an invalid NIP),
    /// or WL2's <c>I</c>, <c>D</c> and <c>X</c>. Only a code of the documented shape is kept (<c>WL-</c> and three digits, or one
    /// upper-case letter); <see langword="null"/> when the answer had none (e.g. a 429 or a 5xx without a body) or had another value.
    /// The ministry publishes the codes in the error-code table of the API's specification.
    /// </summary>
    string? WhiteListCode { get; }
}

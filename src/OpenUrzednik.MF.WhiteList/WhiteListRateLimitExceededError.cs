using System.Net;

using OpenUrzednik.Transport.Http;

namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// The MF White List API answered <c>429 Too Many Requests</c>: the daily per-IP limit is exhausted (code <c>WL-191</c>), or the IP queried
/// too fast (ADR-0023). It is an <see cref="HttpRateLimitExceededError"/>, so <c>is RateLimitExceededError</c>, the status code and
/// <see cref="OpenUrzednik.Core.Errors.RateLimitExceededError.RetryAfter"/> work as for any provider, and it carries the API's code.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="OpenUrzednik.Core.Errors.RateLimitExceededError.RetryAfter"/> is the server's <c>Retry-After</c> when the response has one.
/// Otherwise the client sets it to the time until the next midnight in Warsaw. That value is an estimate, not a contract: it rests on the
/// ministry's statement that an IP over the limit "may" be blocked until midnight. The blocking is discretionary, its rules for querying
/// too fast aren't published, and the ministry documents no <c>Retry-After</c>. An app that needs certainty reads
/// <see cref="WhiteListCode"/> (<c>WL-191</c>) and decides itself.
/// </para>
/// <para>A retry before <c>RetryAfter</c> spends quota and may extend the block.</para>
/// </remarks>
public sealed class WhiteListRateLimitExceededError : HttpRateLimitExceededError, IWhiteListErrorCode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WhiteListRateLimitExceededError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="retryAfter">How long to wait before retrying: the response's <c>Retry-After</c>, or the estimate described on the type.</param>
    /// <param name="statusCode">The status code of the HTTP response that caused the error.</param>
    /// <param name="whiteListCode">The API's error code, or <see langword="null"/> when the answer had none.</param>
    public WhiteListRateLimitExceededError(string message, TimeSpan? retryAfter, HttpStatusCode statusCode, string? whiteListCode)
        : base(message, retryAfter, statusCode)
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

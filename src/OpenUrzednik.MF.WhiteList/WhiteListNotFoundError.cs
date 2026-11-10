using System.Globalization;

using OpenUrzednik.Core.Errors;

namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// The register has no entity for the identifier on the date (ADR-0023): a search with no subject, or WL2's status <c>B</c> ("not in the
/// VAT register"). The answer was a success, so it derives from <see cref="NotFoundError"/>, and <c>is NotFoundError</c> handles it like
/// any other provider's. It keeps the proof of the check, <see cref="RequestId"/> and <see cref="RequestDateTime"/>.
/// </summary>
/// <remarks>
/// The ministry stores every query; <see cref="RequestId"/> may confirm before the tax authorities that the check was made, and the ministry
/// advises keeping it. <see cref="OpenUrzednikError.Metadata"/> holds the proof under <see cref="RequestIdMetadataKey"/>
/// and <see cref="RequestDateTimeMetadataKey"/>.
/// </remarks>
public sealed class WhiteListNotFoundError : NotFoundError
{
    /// <summary>The <see cref="OpenUrzednikError.Metadata"/> key of <see cref="RequestId"/>, a <see cref="string"/>.</summary>
    public const string RequestIdMetadataKey = "requestId";

    /// <summary>
    /// The <see cref="OpenUrzednikError.Metadata"/> key of <see cref="RequestDateTime"/>, an ISO 8601 string with the offset
    /// (e.g. <c>"2026-11-08T14:05:09+01:00"</c>), as Core writes a <see cref="DateTimeOffset"/>.
    /// </summary>
    public const string RequestDateTimeMetadataKey = "requestDateTime";

    /// <summary>
    /// Initializes a new instance of the <see cref="WhiteListNotFoundError"/> class.
    /// </summary>
    /// <param name="message">The message associated with the error.</param>
    /// <param name="requestId">The API's id of the query, the proof that it was made.</param>
    /// <param name="requestDateTime">When the API answered, with the Warsaw offset.</param>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <see langword="null"/>.</exception>
    public WhiteListNotFoundError(string message, string requestId, DateTimeOffset requestDateTime)
        : base(message)
    {
        ArgumentNullException.ThrowIfNull(requestId);

        RequestId = requestId;
        RequestDateTime = requestDateTime;
    }

    /// <summary>
    /// Gets the API's id of the query (<c>requestId</c>), the proof that the check was made.
    /// </summary>
    public string RequestId { get; }

    /// <summary>
    /// Gets when the API answered (<c>requestDateTime</c>): the API's local time in Warsaw with its offset; the hour repeated when daylight
    /// saving time ends gets the standard offset (<c>+01:00</c>).
    /// </summary>
    public DateTimeOffset RequestDateTime { get; }

    /// <inheritdoc />
    protected override void CollectMetadata(IDictionary<string, object?> metadata)
    {
        // Stryker disable once Statement : equivalent, NotFoundError adds no metadata of its own today; the call keeps a later condition key
        base.CollectMetadata(metadata);
        metadata[RequestIdMetadataKey] = RequestId;
        // ISO 8601 with the offset, as Core writes a DateTimeOffset (ADR-0021).
        metadata[RequestDateTimeMetadataKey] = RequestDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture);
    }
}

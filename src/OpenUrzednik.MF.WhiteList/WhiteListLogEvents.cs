namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// The ids of the log events the MF White List clients write (ADR-0003). Each event's name is the name of its constant,
/// e.g. <c>EventId(1001, "RequestTimedOut")</c> in Microsoft.Extensions.Logging.
/// </summary>
/// <remarks>
/// The ids are the same in every MF White List client and are part of the telemetry contract: within a major version an id is never
/// renumbered or reused. 1xxx are HTTP events, 2xxx MF White List-specific ones. Filter by id rather than by message text, which may change.
/// No event carries an identifier (NIP, REGON, account number) unmasked: request paths are written as display paths (ADR-0019).
/// </remarks>
public static class WhiteListLogEvents
{
    /// <summary>A request timed out (the request deadline, <see cref="HttpClient.Timeout"/> or a resilience handler's timeout). Warning.</summary>
    public const int RequestTimedOut = 1001;

    /// <summary>The MF White List API could not be reached (DNS, TLS or connection failure, or a broken body read). Warning.</summary>
    public const int ConnectionFailed = 1002;

    /// <summary>A resilience handler rejected the request (open circuit breaker, rate limiter). Warning.</summary>
    public const int RequestRejected = 1003;

    /// <summary>The API returned 404. Debug.</summary>
    public const int NotFound = 1101;

    /// <summary>
    /// The API returned 400. Warning. Mirrored for completeness: the MF White List error mapping handles every 400 and writes
    /// <see cref="ErrorResponse"/> instead, so the White List clients don't write it today.
    /// </summary>
    public const int BadRequest = 1102;

    /// <summary>The API returned 401 or 403. Warning.</summary>
    public const int Unauthorized = 1103;

    /// <summary>The API returned 408. Warning.</summary>
    public const int RequestTimeoutStatus = 1104;

    /// <summary>
    /// The API returned 429. Warning. Mirrored for completeness: the MF White List error mapping handles every 429 and writes
    /// <see cref="ErrorResponse"/> instead, so the White List clients don't write it today.
    /// </summary>
    public const int RateLimited = 1105;

    /// <summary>
    /// The API returned a 5xx status. Warning. Mirrored for completeness: the MF White List error mapping handles every 5xx and writes
    /// <see cref="ErrorResponse"/> instead, so the White List clients don't write it today.
    /// </summary>
    public const int ServiceUnavailable = 1106;

    /// <summary>The API returned another non-success status. Warning.</summary>
    public const int UnexpectedStatus = 1107;

    /// <summary>
    /// The MF White List error mapping turned a 400, 429 or 5xx response into one of the provider's error types, with the API's code when
    /// the body had one of the documented shape. Warning. Written for every such response, instead of <see cref="BadRequest"/>,
    /// <see cref="RateLimited"/> and <see cref="ServiceUnavailable"/>.
    /// </summary>
    public const int ErrorResponse = 1108;

    /// <summary>The body of an error response could not be read. Debug.</summary>
    public const int ErrorBodyUnreadable = 1109;

    /// <summary>A success response had no body or a <c>null</c> payload. Error.</summary>
    public const int EmptyResponse = 1201;

    /// <summary>A success response was not valid JSON for the expected payload. Error.</summary>
    public const int DeserializationFailed = 1202;

    /// <summary>A success response declared a charset that can't be decoded. Error.</summary>
    public const int DecodingFailed = 1203;

    /// <summary>The arguments failed validation, so no request was sent. Debug.</summary>
    public const int ValidationFailed = 2001;

    /// <summary>The register has no entity for the identifier: the answer is a <see cref="WhiteListNotFoundError"/> with the proof. Debug.</summary>
    public const int NotOnList = 2002;

    /// <summary>A payload was missing data the model needs. Error.</summary>
    public const int MappingFailed = 2003;
}

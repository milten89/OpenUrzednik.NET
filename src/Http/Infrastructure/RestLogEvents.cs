using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Http.Infrastructure;

/// <summary>
/// The log events of the shared HTTP layer (ADR-0003). Ids 1000–1999 belong to it; providers number theirs from 2000.
/// Every provider lists them in its public log-events class (e.g. <c>NbpLogEvents</c>) with the same ids and names.
/// Never renumber or reuse an id.
/// </summary>
internal static class RestLogEvents
{
    // 10xx: the request didn't get a response.
    internal static readonly OpenUrzednikEventId RequestTimedOut = new(1001, nameof(RequestTimedOut));
    internal static readonly OpenUrzednikEventId ConnectionFailed = new(1002, nameof(ConnectionFailed));
    internal static readonly OpenUrzednikEventId RequestRejected = new(1003, nameof(RequestRejected));

    // 11xx: a non-success status.
    internal static readonly OpenUrzednikEventId NotFound = new(1101, nameof(NotFound));
    internal static readonly OpenUrzednikEventId BadRequest = new(1102, nameof(BadRequest));
    internal static readonly OpenUrzednikEventId Unauthorized = new(1103, nameof(Unauthorized));
    internal static readonly OpenUrzednikEventId RequestTimeoutStatus = new(1104, nameof(RequestTimeoutStatus));
    internal static readonly OpenUrzednikEventId RateLimited = new(1105, nameof(RateLimited));
    internal static readonly OpenUrzednikEventId ServiceUnavailable = new(1106, nameof(ServiceUnavailable));
    internal static readonly OpenUrzednikEventId UnexpectedStatus = new(1107, nameof(UnexpectedStatus));
    internal static readonly OpenUrzednikEventId ErrorResponse = new(1108, nameof(ErrorResponse));
    internal static readonly OpenUrzednikEventId ErrorBodyUnreadable = new(1109, nameof(ErrorBodyUnreadable));

    // 12xx: a success status with a body that can't be used.
    internal static readonly OpenUrzednikEventId EmptyResponse = new(1201, nameof(EmptyResponse));
    internal static readonly OpenUrzednikEventId DeserializationFailed = new(1202, nameof(DeserializationFailed));
    internal static readonly OpenUrzednikEventId DecodingFailed = new(1203, nameof(DecodingFailed));
}

using OpenUrzednik.Core;

namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// Asks the Ministry of Finance VAT White List status service (WL2, the API's <c>api2/</c> path) for the VAT status of a NIP: active,
/// exempt or not registered, on a day.
/// </summary>
/// <remarks>
/// <para>
/// The service answers one question, the status, under its own quota of 50,000 queries a day per IP (the search and check of
/// <see cref="IWhiteListClient"/> have others). Every answer carries the proof of the check, <c>RequestId</c> and <c>RequestDateTime</c>: on
/// the result, or, when the NIP isn't in the VAT register, on the <see cref="WhiteListNotFoundError"/>. The ministry stores every query;
/// the <c>RequestId</c> may confirm before the tax authorities that the check was made, and the ministry advises keeping it. The library
/// gives no legal advice.
/// </para>
/// <para>
/// The date is optional, unlike on <see cref="IWhiteListClient"/>. Only its date part is used (time of day and <see cref="DateTime.Kind"/>
/// are ignored). When it is given, it must not be later than today and not earlier than 1 January of the fifth year back, both by the
/// Warsaw calendar, as for the search; another date is a <see cref="OpenUrzednik.Core.Errors.ValidationError{T}"/> of
/// <see cref="DateTime"/>, and no request is sent. The ministry doesn't document the status service's range; the test host enforced this
/// one on 2026-11-21, and the service's own rejection (<see cref="WhiteListQueryRejectedError"/>) covers a later difference.
/// </para>
/// <para>
/// An invalid NIP (length, characters, check digit) is a <see cref="OpenUrzednik.Core.Errors.ValidationError{T}"/> of
/// <see cref="string"/> with the value masked, and no request is sent. The NIP never appears unmasked in the library's own logs, spans,
/// metrics or error messages: requests are written with the route template <c>status/nip/{nip}</c>. .NET's own
/// <see cref="System.Net.Http.HttpClient"/> tracing still records the path (<c>url.full</c>), and so do <c>IHttpClientFactory</c>'s
/// default loggers unless the client is registered through the DI package's display-path logging.
/// </para>
/// <para>
/// The service answers every outcome with HTTP 200 and a status code; the library turns the codes that aren't a status into errors, so
/// code that switches on <see cref="VatStatusResult.VatStatus"/> never takes a failed query for an answer: <c>B</c> is a
/// <see cref="WhiteListNotFoundError"/>, <c>I</c> and <c>D</c> a <see cref="WhiteListQueryRejectedError"/> and <c>X</c> (service inactive)
/// a <see cref="WhiteListServiceInactiveError"/>. The HTTP failures are as on <see cref="IWhiteListClient"/>, e.g. a
/// <see cref="WhiteListRateLimitExceededError"/> for <c>WL-191</c>.
/// </para>
/// <para>
/// Minor versions may add methods to this interface (ADR-0011). If you implement it yourself, e.g. as a decorator, a new method breaks
/// your build until you add it; a <c>throw new NotImplementedException();</c> stub is enough for the methods your code doesn't call.
/// Calling and mocking the interface aren't affected.
/// </para>
/// </remarks>
public interface IWhiteListStatusClient
{
    /// <summary>
    /// Gets the VAT status of a NIP on a date (<c>status/nip/{nip}</c>).
    /// </summary>
    /// <param name="nip">The NIP: 10 digits, with or without spaces, hyphens and the <c>PL</c> prefix.</param>
    /// <param name="date">The day to ask for: from 1 January of the fifth year back to today (Europe/Warsaw); <see langword="null"/> asks for
    /// today, and <see cref="VatStatusResult.Date"/> then says which day the API answered for.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    /// The status with the proof of the check; a <see cref="WhiteListNotFoundError"/> with the proof when the NIP isn't in the VAT register;
    /// a validation error for an invalid NIP or date; or another error.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="nip"/> is <see langword="null"/>.</exception>
    Task<OpenUrzednikResult<VatStatusResult>> GetVatStatusByNipAsync(string nip, DateTime? date = null, CancellationToken cancellationToken = default);
}

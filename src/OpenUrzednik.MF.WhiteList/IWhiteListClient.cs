using OpenUrzednik.Core;
using OpenUrzednik.MF.WhiteList.Search;

namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// Searches the Ministry of Finance VAT White List (Wykaz podatników VAT, the API's <c>api/</c> path): the subject with a NIP or REGON, or
/// the subjects with an account number, on a date; one identifier at a time or up to 30 in one batch.
/// </summary>
/// <remarks>
/// <para>
/// Every answer of the register carries the proof of the check, <c>RequestId</c> and <c>RequestDateTime</c>: on the result, or, when the
/// subject isn't on the list, on the <see cref="WhiteListNotFoundError"/>. The ministry stores every query; the <c>RequestId</c> may confirm
/// before the tax authorities that the check was made, and the ministry advises keeping it. The library gives no legal advice.
/// </para>
/// <para>
/// The date is required, as in the API, because it is part of what the proof proves. Only its date part is used (time of day and
/// <see cref="DateTime.Kind"/> are ignored). It must not be later than today and not earlier than 1 January of the fifth year back, both by
/// the Warsaw calendar (art. 96b ust. 2 of the VAT act); another date is a <see cref="OpenUrzednik.Core.Errors.ValidationError{T}"/> of
/// <see cref="DateTime"/>, and no request is sent.
/// </para>
/// <para>
/// Identifiers may carry spaces and hyphens, and a NIP or account number the <c>PL</c> prefix; the client sends the plain digits. An
/// invalid identifier (length, characters, check digit) is a <see cref="OpenUrzednik.Core.Errors.ValidationError{T}"/> of
/// <see cref="string"/> with the value masked, and no request is sent. Identifiers never appear unmasked in the library's own logs,
/// spans, metrics or error messages: requests are written with route templates such as <c>search/nip/{nip}</c>. .NET's own
/// <see cref="System.Net.Http.HttpClient"/> tracing still records the path (<c>url.full</c>), and so do <c>IHttpClientFactory</c>'s
/// default loggers unless the client is registered through the DI package's display-path logging.
/// </para>
/// <para>
/// The ministry limits each IP to 100 searches a day and may block an IP over the limit, or one that queries too fast, until midnight;
/// the API then answers 429 (<c>WL-191</c>), a <see cref="WhiteListRateLimitExceededError"/>. The list is updated on working days, with
/// possible disruption between 22:00 and 24:00 (<c>WL-195</c>, <c>WL-196</c>: a <see cref="WhiteListServiceUnavailableError"/>; repeat the
/// query).
/// </para>
/// <para>
/// Minor versions may add methods to this interface (ADR-0011). If you implement it yourself, e.g. as a decorator, a new method breaks
/// your build until you add it; a <c>throw new NotImplementedException();</c> stub is enough for the methods your code doesn't call.
/// Calling and mocking the interface aren't affected.
/// </para>
/// </remarks>
public interface IWhiteListClient
{
    /// <summary>
    /// Searches the subject with a NIP on a date (<c>search/nip/{nip}</c>).
    /// </summary>
    /// <param name="nip">The NIP: 10 digits, with or without spaces, hyphens and the <c>PL</c> prefix.</param>
    /// <param name="date">The date of the check: from 1 January of the fifth year back to today (Europe/Warsaw).</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    /// The subject with the proof of the check; a <see cref="WhiteListNotFoundError"/> with the proof when no subject on the list has the
    /// NIP on that date; a validation error for an invalid NIP or date; or another error.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="nip"/> is <see langword="null"/>.</exception>
    Task<OpenUrzednikResult<SubjectSearchResult>> SearchByNipAsync(string nip, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the subject with a REGON on a date (<c>search/regon/{regon}</c>).
    /// </summary>
    /// <param name="regon">The REGON: 9 or 14 digits, with or without spaces and hyphens.</param>
    /// <param name="date">The date of the check: from 1 January of the fifth year back to today (Europe/Warsaw).</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    /// The subject with the proof of the check; a <see cref="WhiteListNotFoundError"/> with the proof when no subject on the list has the
    /// REGON on that date; a validation error for an invalid REGON or date; or another error.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="regon"/> is <see langword="null"/>.</exception>
    Task<OpenUrzednikResult<SubjectSearchResult>> SearchByRegonAsync(string regon, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the subjects with an account number on a date (<c>search/bank-account/{account}</c>). More than one subject can share
    /// an account.
    /// </summary>
    /// <remarks>
    /// A subject with virtual accounts (<see cref="Subject.HasVirtualAccounts"/>) may be paid through an account the list doesn't show, so
    /// this search doesn't find it by that account.
    /// </remarks>
    /// <param name="bankAccount">The account number (NRB): 26 digits, with or without spaces, hyphens and the <c>PL</c> prefix.</param>
    /// <param name="date">The date of the check: from 1 January of the fifth year back to today (Europe/Warsaw).</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    /// The subjects with the proof of the check; a <see cref="WhiteListNotFoundError"/> with the proof when no subject on the list has the
    /// account on that date; a validation error for an invalid account number or date; or another error.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="bankAccount"/> is <see langword="null"/>.</exception>
    Task<OpenUrzednikResult<SubjectsSearchResult>> SearchByBankAccountAsync(string bankAccount, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the subjects with each of up to 30 NIPs on a date, in one request (<c>search/nips/{nips}</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The call succeeds when the API answers: the result has one <see cref="BatchSearchEntry"/> per NIP, in the order given, each with its
    /// subject or its own error (<see cref="WhiteListNotFoundError"/> with the batch's proof when the NIP isn't on the list). The batch's
    /// proof, <see cref="BatchSearchResult.RequestId"/>, covers every entry. Duplicates are sent, and each gets its entry.
    /// </para>
    /// <para>
    /// Every NIP is validated first: an invalid one is a <see cref="OpenUrzednik.Core.Errors.ValidationError{T}"/> of <see cref="string"/>
    /// named after its position (<c>nips[3]</c>, counted from 0), with the value masked; an empty list or more than 30 NIPs is a
    /// <see cref="OpenUrzednik.Core.Errors.ValidationError{T}"/> of <see cref="int"/> (the count) named <c>nips</c>. The call returns all of
    /// them, with the date's, and sends no request. The ministry doesn't say whether a batch counts as one or as up to 30 of an IP's 100
    /// daily searches.
    /// </para>
    /// </remarks>
    /// <param name="nips">1 to 30 NIPs: 10 digits each, with or without spaces, hyphens and the <c>PL</c> prefix.</param>
    /// <param name="date">The date of the check: from 1 January of the fifth year back to today (Europe/Warsaw).</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    /// The entries with the proof of the check; validation errors for invalid NIPs, their count or the date; or another error when the
    /// request fails.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="nips"/> or one of its elements is <see langword="null"/>.</exception>
    Task<OpenUrzednikResult<BatchSearchResult>> SearchByNipsAsync(IReadOnlyList<string> nips, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the subjects with each of up to 30 REGONs on a date, in one request (<c>search/regons/{regons}</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The call succeeds when the API answers: the result has one <see cref="BatchSearchEntry"/> per REGON, in the order given, each with
    /// its subject or its own error (<see cref="WhiteListNotFoundError"/> with the batch's proof when the REGON isn't on the list). The
    /// batch's proof, <see cref="BatchSearchResult.RequestId"/>, covers every entry. Duplicates are sent, and each gets its entry.
    /// </para>
    /// <para>
    /// Every REGON is validated first: an invalid one is a <see cref="OpenUrzednik.Core.Errors.ValidationError{T}"/> of <see cref="string"/>
    /// named after its position (<c>regons[3]</c>, counted from 0), with the value masked; an empty list or more than 30 REGONs is a
    /// <see cref="OpenUrzednik.Core.Errors.ValidationError{T}"/> of <see cref="int"/> (the count) named <c>regons</c>. The call returns
    /// all of them, with the date's, and sends no request. The ministry doesn't say whether a batch counts as one or as up to 30 of an
    /// IP's 100 daily searches.
    /// </para>
    /// </remarks>
    /// <param name="regons">1 to 30 REGONs: 9 or 14 digits each, with or without spaces and hyphens.</param>
    /// <param name="date">The date of the check: from 1 January of the fifth year back to today (Europe/Warsaw).</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    /// The entries with the proof of the check; validation errors for invalid REGONs, their count or the date; or another error when the
    /// request fails.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="regons"/> or one of its elements is <see langword="null"/>.</exception>
    Task<OpenUrzednikResult<BatchSearchResult>> SearchByRegonsAsync(IReadOnlyList<string> regons, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the subjects with each of up to 30 account numbers on a date, in one request (<c>search/bank-accounts/{accounts}</c>).
    /// More than one subject can share an account.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The call succeeds when the API answers: the result has one <see cref="BatchSearchEntry"/> per account number, in the order given,
    /// each with its subjects or its own error (<see cref="WhiteListNotFoundError"/> with the batch's proof when no subject on the list has
    /// the account). The batch's proof, <see cref="BatchSearchResult.RequestId"/>, covers every entry. Duplicates are sent, and each gets
    /// its entry. A subject with virtual accounts (<see cref="Subject.HasVirtualAccounts"/>) may be paid through an account the list
    /// doesn't show, so this search doesn't find it by that account.
    /// </para>
    /// <para>
    /// Every account number is validated first: an invalid one is a <see cref="OpenUrzednik.Core.Errors.ValidationError{T}"/> of
    /// <see cref="string"/> named after its position (<c>bankAccounts[3]</c>, counted from 0), with the value masked; an empty list or more
    /// than 30 account numbers is a <see cref="OpenUrzednik.Core.Errors.ValidationError{T}"/> of <see cref="int"/> (the count) named
    /// <c>bankAccounts</c>. The call returns all of them, with the date's, and sends no request. The ministry doesn't say whether a batch
    /// counts as one or as up to 30 of an IP's 100 daily searches.
    /// </para>
    /// </remarks>
    /// <param name="bankAccounts">1 to 30 account numbers (NRB): 26 digits each, with or without spaces, hyphens and the <c>PL</c> prefix.</param>
    /// <param name="date">The date of the check: from 1 January of the fifth year back to today (Europe/Warsaw).</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    /// The entries with the proof of the check; validation errors for invalid account numbers, their count or the date; or another error
    /// when the request fails.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="bankAccounts"/> or one of its elements is <see langword="null"/>.</exception>
    Task<OpenUrzednikResult<BatchSearchResult>> SearchByBankAccountsAsync(IReadOnlyList<string> bankAccounts, DateTime date, CancellationToken cancellationToken = default);
}

using OpenUrzednik.MF.WhiteList.Search;

namespace OpenUrzednik.MF.WhiteList.Status;

/// <summary>
/// The answer of the WL2 status service: a NIP's status as a VAT taxpayer on a day, with the proof of the check.
/// </summary>
/// <remarks>
/// <para>
/// The ministry stores every query; <paramref name="RequestId"/> may confirm before the tax authorities that the check was made, and the
/// ministry advises keeping it (ADR-0023). The library gives no legal advice.
/// </para>
/// <para>
/// A NIP that isn't in the VAT register is no status: it is a <see cref="WhiteListNotFoundError"/> with the proof, as on the search
/// client. A rejected query or an inactive service is an error too, never a status.
/// </para>
/// </remarks>
/// <param name="VatStatus">The NIP's status on <paramref name="Date"/>.</param>
/// <param name="Date">
/// The day the status is for (<c>requestedDate</c>): the date asked for, or today (Europe/Warsaw) when the query had none. Midnight,
/// <see cref="DateTimeKind.Unspecified"/>.
/// </param>
/// <param name="RequestId">The API's id of the query (<c>requestId</c>), the proof that the check was made.</param>
/// <param name="RequestDateTime">
/// When the API answered (<c>responseDateTime</c>): the API's local time in Warsaw with its offset; the hour repeated when daylight
/// saving time ends gets the standard offset (<c>+01:00</c>).
/// </param>
public sealed record VatStatusResult(VatStatus VatStatus, DateTime Date, string RequestId, DateTimeOffset RequestDateTime);

namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// The answer of a check whether an account number belongs to a VAT taxpayer: the verdict, with the proof of the check.
/// </summary>
/// <remarks>
/// <para>
/// The ministry stores every query; <paramref name="RequestId"/> may confirm before the tax authorities that the check was made on that
/// day, and the ministry advises keeping it (ADR-0023). The library gives no legal advice.
/// </para>
/// <para>
/// A check has no "not on the list" outcome: an entity that isn't on the list, or isn't an active VAT taxpayer on the date, gets
/// <paramref name="AccountAssigned"/> <see langword="false"/> like an account that isn't assigned to it.
/// </para>
/// </remarks>
/// <param name="AccountAssigned">
/// <see langword="true"/> (the API's <c>TAK</c>) when the account is on the list for that active VAT taxpayer on the date;
/// <see langword="false"/> (<c>NIE</c>) when it isn't.
/// </param>
/// <param name="RequestId">The API's id of the query (<c>requestId</c>), the proof that the check was made.</param>
/// <param name="RequestDateTime">
/// When the API answered (<c>requestDateTime</c>): the API's local time in Warsaw with its offset; the hour repeated when daylight
/// saving time ends gets the standard offset (<c>+01:00</c>).
/// </param>
public sealed record AccountCheckResult(bool AccountAssigned, string RequestId, DateTimeOffset RequestDateTime);

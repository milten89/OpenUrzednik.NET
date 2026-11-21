namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// The answer of a search by one NIP or REGON: the subject on the list, with the proof of the check.
/// </summary>
/// <remarks>
/// The ministry stores every query; <paramref name="RequestId"/> may confirm before the tax authorities that the check was made on that
/// day, and the ministry advises keeping it (ADR-0023). A subject that isn't on the list is a <see cref="WhiteListNotFoundError"/>, which
/// keeps the same proof.
/// </remarks>
/// <param name="Subject">The subject on the requested date.</param>
/// <param name="RequestId">The API's id of the query (<c>requestId</c>), the proof that the check was made.</param>
/// <param name="RequestDateTime">
/// When the API answered (<c>requestDateTime</c>): the API's local time in Warsaw with its offset; the hour repeated when daylight
/// saving time ends gets the standard offset (<c>+01:00</c>).
/// </param>
public sealed record SubjectSearchResult(Subject Subject, string RequestId, DateTimeOffset RequestDateTime);

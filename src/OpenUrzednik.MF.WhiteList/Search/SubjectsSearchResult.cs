namespace OpenUrzednik.MF.WhiteList.Search;

/// <summary>
/// The answer of a search by one account number: the subjects on the list that have the account, with the proof of the check.
/// </summary>
/// <remarks>
/// The ministry stores every query; <paramref name="RequestId"/> may confirm before the tax authorities that the check was made on that
/// day, and the ministry advises keeping it (ADR-0023). An account that no subject on the list has is a
/// <see cref="WhiteListNotFoundError"/>, which keeps the same proof, so <paramref name="Subjects"/> is never empty.
/// </remarks>
/// <param name="Subjects">The subjects with the account on the requested date, at least one.</param>
/// <param name="RequestId">The API's id of the query (<c>requestId</c>), the proof that the check was made.</param>
/// <param name="RequestDateTime">
/// When the API answered (<c>requestDateTime</c>): the API's local time in Warsaw with its offset; the hour repeated when daylight
/// saving time ends gets the standard offset (<c>+01:00</c>).
/// </param>
public sealed record SubjectsSearchResult(IReadOnlyList<Subject> Subjects, string RequestId, DateTimeOffset RequestDateTime)
{
    /// <summary>Compares all members, and <c>Subjects</c> item by item (a record would compare the list reference).</summary>
    /// <param name="other">Record to compare with.</param>
    /// <returns><see langword="true"/> if the records are equal.</returns>
    public bool Equals(SubjectsSearchResult? other)
        => other is not null &&
           RequestId == other.RequestId &&
           RequestDateTime == other.RequestDateTime &&
           Subjects.SequenceEqual(other.Subjects);

    /// <inheritdoc/>
    public override int GetHashCode()
        => (RequestId, RequestDateTime, Subjects.Count).GetHashCode();
}

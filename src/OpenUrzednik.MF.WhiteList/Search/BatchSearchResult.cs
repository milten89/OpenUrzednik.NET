namespace OpenUrzednik.MF.WhiteList.Search;

/// <summary>
/// The answer of a batch search by NIPs, REGONs or account numbers: one entry per identifier sent, in the order sent (a duplicate gets
/// its own entry), with the proof of the check for the whole batch.
/// </summary>
/// <remarks>
/// The call succeeds when the API answers; each entry then holds its subjects or its own error (ADR-0023), so check
/// <see cref="BatchSearchEntry.IsSuccess"/> per entry. The ministry stores every query; <paramref name="RequestId"/> may confirm before the
/// tax authorities that the check was made on that day, and the ministry advises keeping it.
/// </remarks>
/// <param name="Entries">The entries, one per identifier sent, in the order sent.</param>
/// <param name="RequestId">The API's id of the query (<c>requestId</c>), the proof that the check was made.</param>
/// <param name="RequestDateTime">
/// When the API answered (<c>requestDateTime</c>): the API's local time in Warsaw with its offset; the hour repeated when daylight
/// saving time ends gets the standard offset (<c>+01:00</c>).
/// </param>
public sealed record BatchSearchResult(IReadOnlyList<BatchSearchEntry> Entries, string RequestId, DateTimeOffset RequestDateTime)
{
    /// <summary>Compares all members, and <c>Entries</c> item by item (a record would compare the list reference).</summary>
    /// <param name="other">Record to compare with.</param>
    /// <returns><see langword="true"/> if the records are equal.</returns>
    public bool Equals(BatchSearchResult? other)
        => other is not null &&
           RequestId == other.RequestId &&
           RequestDateTime == other.RequestDateTime &&
           Entries.SequenceEqual(other.Entries);

    /// <inheritdoc/>
    public override int GetHashCode()
        => (RequestId, RequestDateTime, Entries.Count).GetHashCode();
}

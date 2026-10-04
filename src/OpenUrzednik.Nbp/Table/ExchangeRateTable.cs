namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// NBP table A or B of mid (average) exchange rates
/// </summary>
/// <param name="TableId">Exchange rate table number</param>
/// <param name="PublicationDate">Publication date</param>
/// <param name="Rates">List of exchange rates</param>
public sealed record ExchangeRateTable(string TableId, DateOnly PublicationDate, IReadOnlyList<TableRate> Rates)
{
    /// <summary>Compares all members, and <c>Rates</c> item by item (a record would compare the list reference).</summary>
    /// <param name="other">Record to compare with.</param>
    /// <returns><see langword="true"/> if the records are equal.</returns>
    public bool Equals(ExchangeRateTable? other)
        => other is not null &&
           TableId == other.TableId &&
           PublicationDate == other.PublicationDate &&
           Rates.SequenceEqual(other.Rates);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(TableId, PublicationDate, Rates.Count);
}

namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// Exchange rate table
/// </summary>
/// <param name="TableId">Exchange rate table number</param>
/// <param name="PublicationDate">Publication date</param>
/// <param name="Rates">List of exchange rates</param>
public sealed record ExchangeRateTable(string TableId, DateOnly PublicationDate, IReadOnlyList<ExchangeRate> Rates)
{
    public bool Equals(ExchangeRateTable? other)
        => other is not null &&
           TableId == other.TableId &&
           PublicationDate == other.PublicationDate &&
           Rates.SequenceEqual(other.Rates);

    public override int GetHashCode()
        => HashCode.Combine(TableId, PublicationDate, Rates.Count);
}

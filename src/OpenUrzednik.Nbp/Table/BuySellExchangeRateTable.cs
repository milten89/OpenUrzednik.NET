namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// Buy and sell exchange rate table
/// </summary>
/// <param name="TableId">Exchange rate table number</param>
/// <param name="TradingDate">Trading date</param>
/// <param name="PublicationDate">Publication date</param>
/// <param name="Rates">List of exchange rates</param>
public sealed record BuySellExchangeRateTable(string TableId, DateOnly TradingDate, DateOnly PublicationDate, IReadOnlyList<TableBuySellRate> Rates)
{
    /// <summary>Compares all members, and <c>Rates</c> item by item (a record would compare the list reference).</summary>
    /// <param name="other">Record to compare with.</param>
    /// <returns><see langword="true"/> if the records are equal.</returns>
    public bool Equals(BuySellExchangeRateTable? other)
        => other is not null &&
           TableId == other.TableId &&
           TradingDate == other.TradingDate &&
           PublicationDate == other.PublicationDate &&
           Rates.SequenceEqual(other.Rates);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(TableId, TradingDate, PublicationDate, Rates.Count);
}

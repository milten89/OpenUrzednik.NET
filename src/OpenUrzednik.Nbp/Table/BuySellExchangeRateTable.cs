namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// Buy and sell exchange rate table
/// </summary>
/// <param name="TableId">Exchange rate table number</param>
/// <param name="TradingDate">Traiding date</param>
/// <param name="PublicationDate">Publication date</param>
/// <param name="Rates">List of exchange rates</param>
public sealed record BuySellExchangeRateTable(string TableId, DateOnly TradingDate, DateOnly PublicationDate, IReadOnlyList<BuySellExchangeRate> Rates)
{
    public bool Equals(BuySellExchangeRateTable? other) 
        => other is not null && 
           TableId == other.TableId && 
           TradingDate == other.TradingDate && 
           PublicationDate == other.PublicationDate && 
           Rates.SequenceEqual(other.Rates);

    public override int GetHashCode() 
        => HashCode.Combine(TableId, TradingDate, PublicationDate, Rates.Count);
}

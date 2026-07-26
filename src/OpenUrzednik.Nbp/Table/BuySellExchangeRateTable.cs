namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// Buy and sell exchange rate table
/// </summary>
/// <param name="No">Exchange rate table number</param>
/// <param name="TradingDate">Traiding date</param>
/// <param name="EffectiveDate">Publication date</param>
/// <param name="Rates">List of exchange rates</param>
public sealed record BuySellExchangeRateTable(string No, DateOnly TradingDate, DateOnly EffectiveDate, IReadOnlyList<BuySellExchangeRate> Rates);

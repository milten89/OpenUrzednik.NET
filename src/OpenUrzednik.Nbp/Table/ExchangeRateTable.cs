namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// Exchange rate table
/// </summary>
/// <param name="No">Exchange rate table number</param>
/// <param name="EffectiveDate">Publication date</param>
/// <param name="Rates">List of exchange rates</param>
public sealed record ExchangeRateTable(string No, DateOnly EffectiveDate, IReadOnlyList<ExchangeRate> Rates);

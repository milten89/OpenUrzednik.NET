namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Buy and sell exchange rate
/// </summary>
/// <param name="No">Exchange rate table number</param>
/// <param name="EffectiveDate">Publication date</param>
/// <param name="Bid">Currency buy rate</param>
/// <param name="Ask">Currency sell rate</param>
public sealed record BuySellExchangeRate(string No, DateOnly EffectiveDate, decimal Bid, decimal Ask);
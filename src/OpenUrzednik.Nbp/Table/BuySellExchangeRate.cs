namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// Buy and sell exchange rate
/// </summary>
/// <param name="CurrencyName">Currency name</param>
/// <param name="CurrencyCode">ISO 4217 currency code</param>
/// <param name="Buy">Currency buy rate</param>
/// <param name="Sell">Currency sell rate</param>
public sealed record BuySellExchangeRate(string CurrencyName, string CurrencyCode, decimal Buy, decimal Sell);

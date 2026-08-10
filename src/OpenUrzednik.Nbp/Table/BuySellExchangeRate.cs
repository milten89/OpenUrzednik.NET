namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// Buy and sell exchange rate
/// </summary>
/// <param name="Currency">Currency name</param>
/// <param name="Code">ISO 4217 currency code</param>
/// <param name="Buy">Currency buy rate</param>
/// <param name="Sell">Currency sell rate</param>
public sealed record BuySellExchangeRate(string Currency, string Code, decimal Buy, decimal Sell);

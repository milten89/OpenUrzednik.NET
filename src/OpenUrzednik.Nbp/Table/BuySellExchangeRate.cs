namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// Buy and sell exchange rate
/// </summary>
/// <param name="Currency">Currency name</param>
/// <param name="Code">ISO 4217 currency code</param>
/// <param name="Bid">Currency buy rate</param>
/// <param name="Ask">Currency sell rate</param>
public sealed record BuySellExchangeRate(string Currency, string Code, decimal Bid, decimal Ask);
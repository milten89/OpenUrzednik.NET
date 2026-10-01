namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// One currency's buy and sell rates in NBP table C.
/// </summary>
/// <param name="CurrencyName">Currency name</param>
/// <param name="CurrencyCode">ISO 4217 currency code</param>
/// <param name="Buy">Currency buy rate</param>
/// <param name="Sell">Currency sell rate</param>
public sealed record TableBuySellRate(string CurrencyName, string CurrencyCode, decimal Buy, decimal Sell);

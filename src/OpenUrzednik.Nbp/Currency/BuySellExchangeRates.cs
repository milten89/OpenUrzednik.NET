namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Buy and sell exchange rates
/// </summary>
/// <param name="Currency">Currency name</param>
/// <param name="Code">ISO 4217 currency code</param>
/// <param name="Rates">List of exchange rates</param>
public sealed record BuySellExchangeRates(string Currency, string Code, IReadOnlyList<BuySellExchangeRate> Rates);

namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// Exchange rate
/// </summary>
/// <param name="CurrencyName">Currency name</param>
/// <param name="CurrencyCode">ISO 4217 currency code</param>
/// <param name="Price">Currency average exchange rate</param>
public sealed record ExchangeRate(string CurrencyName, string CurrencyCode, decimal Price);

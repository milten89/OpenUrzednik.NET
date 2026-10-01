namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// One entry of <see cref="ExchangeRateTable.Rates"/>: the mid rate of one currency in the table
/// </summary>
/// <param name="CurrencyName">Currency name</param>
/// <param name="CurrencyCode">ISO 4217 currency code</param>
/// <param name="Price">Currency mid exchange rate</param>
public sealed record TableRate(string CurrencyName, string CurrencyCode, decimal Price);

namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// One entry of <see cref="ExchangeRateTable.Rates"/>: the mid rate of one currency in the table
/// </summary>
/// <param name="CurrencyName">
/// Currency name in Polish, as NBP gives it (e.g. <c>dolar amerykański</c>). Empty in table A published before
/// 2004-05-04 (up to 85/A/NBP/2004): NBP gives the country (e.g. <c>USA</c>) there, not the currency name.
/// Table B always has it.
/// </param>
/// <param name="CurrencyCode">ISO 4217 currency code</param>
/// <param name="Price">Currency mid exchange rate</param>
public sealed record TableRate(string CurrencyName, string CurrencyCode, decimal Price);

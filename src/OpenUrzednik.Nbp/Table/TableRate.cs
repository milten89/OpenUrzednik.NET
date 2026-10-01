namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// One currency's mid exchange rate in an NBP table (table A or B).
/// </summary>
/// <param name="CurrencyName">Currency name</param>
/// <param name="CurrencyCode">ISO 4217 currency code</param>
/// <param name="Price">Currency average exchange rate</param>
public sealed record TableRate(string CurrencyName, string CurrencyCode, decimal Price);

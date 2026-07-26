namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// Exchange rate
/// </summary>
/// <param name="Currency">Currency name</param>
/// <param name="Code">ISO 4217 currency code</param>
/// <param name="Mid">Currency average exchange rate</param>
public sealed record ExchangeRate(string Currency, string Code, decimal Mid);

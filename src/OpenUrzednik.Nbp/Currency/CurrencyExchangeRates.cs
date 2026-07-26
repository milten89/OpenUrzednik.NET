namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Currency exchange rates
/// </summary>
/// <param name="Currency">Currency name</param>
/// <param name="Code">ISO 4217 currency code</param>
/// <param name="Rates">List of exchange rates</param>
public sealed record CurrencyExchangeRates(string Currency, string Code, IReadOnlyList<ExchangeRate> Rates);

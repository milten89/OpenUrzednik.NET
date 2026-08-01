namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Country currency exchange rates
/// </summary>
/// <param name="Country">Country name</param>
/// <param name="CurrencyName">Currency name</param>
/// <param name="CurrencySymbol">ISO 4217 currency symbol</param>
/// <param name="CurrencyCode">ISO 4217 currency code</param>
/// <param name="Rates">List of exchange rates</param>
public sealed record CountryExchangeRates(string Country, string CurrencyName, string CurrencySymbol, string CurrencyCode, IReadOnlyList<ExchangeRate> Rates);

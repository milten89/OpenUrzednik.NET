namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Country currency exchange rates
/// </summary>
/// <param name="Country">Country name</param>
/// <param name="Symbol">ISO 4217 currency symbol</param>
/// <param name="Currency">Currency name</param>
/// <param name="Code">ISO 4217 currency code</param>
/// <param name="Rates">List of exchange rates</param>
public sealed record CountryExchangeRates(string Country, int Symbol, string Currency, string Code, IReadOnlyList<ExchangeRate> Rates);

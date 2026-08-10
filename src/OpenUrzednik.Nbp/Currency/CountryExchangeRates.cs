namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Country currency exchange rates
/// </summary>
/// <param name="Country">Country name</param>
/// <param name="CurrencyName">Currency name</param>
/// <param name="CurrencySymbol">ISO 4217 currency symbol</param>
/// <param name="CurrencyCode">ISO 4217 currency code</param>
/// <param name="Rates">List of exchange rates</param>
public sealed record CountryExchangeRates(string Country, string CurrencyName, string CurrencySymbol, string CurrencyCode, IReadOnlyList<ExchangeRate> Rates)
{
    public bool Equals(CountryExchangeRates? other)
        => other is not null &&
           Country == other.Country &&
           CurrencyName == other.CurrencyName &&
           CurrencySymbol == other.CurrencySymbol &&
           CurrencyCode == other.CurrencyCode &&
           Rates.SequenceEqual(other.Rates);

    public override int GetHashCode()
        => HashCode.Combine(Country, CurrencyName, CurrencySymbol, CurrencyCode, Rates.Count);
}

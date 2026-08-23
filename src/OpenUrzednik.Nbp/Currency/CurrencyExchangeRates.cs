namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Currency exchange rates
/// </summary>
/// <param name="CurrencyName">Currency name</param>
/// <param name="CurrencyCode">ISO 4217 currency code</param>
/// <param name="Rates">List of exchange rates</param>
public sealed record CurrencyExchangeRates(string CurrencyName, string CurrencyCode, IReadOnlyList<ExchangeRate> Rates)
{
    public bool Equals(CurrencyExchangeRates? other)
        => other is not null &&
           CurrencyName == other.CurrencyName &&
           CurrencyCode == other.CurrencyCode &&
           Rates.SequenceEqual(other.Rates);

    public override int GetHashCode()
        => HashCode.Combine(CurrencyName, CurrencyCode, Rates.Count);
}

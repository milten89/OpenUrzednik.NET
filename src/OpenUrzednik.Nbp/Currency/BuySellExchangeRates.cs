namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Bid and ask rates of one currency from NBP table C
/// </summary>
/// <param name="CurrencyName">Currency name</param>
/// <param name="CurrencyCode">ISO 4217 currency code</param>
/// <param name="Rates">List of exchange rates</param>
public sealed record BuySellExchangeRates(string CurrencyName, string CurrencyCode, IReadOnlyList<CurrencyBuySellRate> Rates)
{
    public bool Equals(BuySellExchangeRates? other)
        => other is not null &&
           CurrencyName == other.CurrencyName &&
           CurrencyCode == other.CurrencyCode &&
           Rates.SequenceEqual(other.Rates);

    public override int GetHashCode()
        => HashCode.Combine(CurrencyName, CurrencyCode, Rates.Count);
}

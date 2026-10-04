namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Mid (NBP average) exchange rates of one currency from NBP table A or B
/// </summary>
/// <param name="CurrencyName">Currency name</param>
/// <param name="CurrencyCode">ISO 4217 currency code</param>
/// <param name="Rates">List of exchange rates</param>
public sealed record CurrencyExchangeRates(string CurrencyName, string CurrencyCode, IReadOnlyList<CurrencyRate> Rates)
{
    /// <summary>Compares all members, and <c>Rates</c> item by item (a record would compare the list reference).</summary>
    /// <param name="other">Record to compare with.</param>
    /// <returns><see langword="true"/> if the records are equal.</returns>
    public bool Equals(CurrencyExchangeRates? other)
        => other is not null &&
           CurrencyName == other.CurrencyName &&
           CurrencyCode == other.CurrencyCode &&
           Rates.SequenceEqual(other.Rates);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(CurrencyName, CurrencyCode, Rates.Count);
}

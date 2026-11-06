namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Mid (NBP average) exchange rates of one currency from NBP table A or B
/// </summary>
/// <param name="CurrencyName">
/// Currency name in Polish, as NBP gives it (e.g. <c>dolar amerykański</c>). Empty for table A when the earliest rate in
/// the answer was published before 2004-05-04 (up to 85/A/NBP/2004): NBP then gives the country, not the currency name,
/// for the whole answer, so a range, or the lookback window of <c>GetLastPublishedBeforeAsync</c>,
/// that crosses that date has no name for its later rates either. Table B always has it.
/// </param>
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

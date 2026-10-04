namespace OpenUrzednik.Nbp.Table;

/// <summary>
/// One entry of <see cref="BuySellExchangeRateTable.Rates"/>: the bid and ask rates of one currency in the table
/// </summary>
/// <param name="CurrencyName">Currency name</param>
/// <param name="CurrencyCode">ISO 4217 currency code</param>
/// <param name="Ask">
/// Ask rate, the API's <c>ask</c> (NBP's selling rate, <i>kurs sprzedaży</i>): what you pay when you buy the currency.
/// The higher of the two rates.
/// </param>
/// <param name="Bid">
/// Bid rate, the API's <c>bid</c> (NBP's buying rate, <i>kurs kupna</i>): what you get when you sell the currency.
/// </param>
public sealed record TableBuySellRate(string CurrencyName, string CurrencyCode, decimal Ask, decimal Bid);

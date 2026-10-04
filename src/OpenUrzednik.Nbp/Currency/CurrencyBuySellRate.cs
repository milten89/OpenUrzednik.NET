namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// One entry of <see cref="BuySellExchangeRates.Rates"/>: the bid and ask rates of the currency in one NBP table C
/// </summary>
/// <param name="TableId">Exchange rate table number</param>
/// <param name="PublicationDate">Publication date</param>
/// <param name="Ask">
/// Ask rate, the API's <c>ask</c> (NBP's selling rate, <i>kurs sprzedaży</i>): what you pay when you buy the currency.
/// The higher of the two rates.
/// </param>
/// <param name="Bid">
/// Bid rate, the API's <c>bid</c> (NBP's buying rate, <i>kurs kupna</i>): what you get when you sell the currency.
/// </param>
public sealed record CurrencyBuySellRate(string TableId, DateOnly PublicationDate, decimal Ask, decimal Bid);

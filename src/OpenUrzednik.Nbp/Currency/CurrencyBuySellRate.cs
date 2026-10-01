namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// One entry of <see cref="BuySellExchangeRates.Rates"/>: the buy and sell rates of the currency in one NBP table C
/// </summary>
/// <param name="TableId">Exchange rate table number</param>
/// <param name="PublicationDate">Publication date</param>
/// <param name="Buy">Currency buy rate</param>
/// <param name="Sell">Currency sell rate</param>
public sealed record CurrencyBuySellRate(string TableId, DateOnly PublicationDate, decimal Buy, decimal Sell);

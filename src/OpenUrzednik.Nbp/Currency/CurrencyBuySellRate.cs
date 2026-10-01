namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Buy and sell rates of one currency, as published in one NBP table C.
/// </summary>
/// <param name="TableId">Exchange rate table number</param>
/// <param name="PublicationDate">Publication date</param>
/// <param name="Buy">Currency buy rate</param>
/// <param name="Sell">Currency sell rate</param>
public sealed record CurrencyBuySellRate(string TableId, DateOnly PublicationDate, decimal Buy, decimal Sell);

namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// One entry of <see cref="CurrencyExchangeRates.Rates"/>: the mid rate of the currency in one NBP table (A or B)
/// </summary>
/// <param name="TableId">Exchange rate table number</param>
/// <param name="PublicationDate">Publication date</param>
/// <param name="Price">Currency mid exchange rate</param>
public sealed record CurrencyRate(string TableId, DateOnly PublicationDate, decimal Price);

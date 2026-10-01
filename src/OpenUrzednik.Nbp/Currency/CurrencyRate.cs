namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Mid exchange rate of one currency, as published in one NBP table (table A or B).
/// </summary>
/// <param name="TableId">Exchange rate table number</param>
/// <param name="PublicationDate">Publication date</param>
/// <param name="Price">Currency average exchange rate</param>
public sealed record CurrencyRate(string TableId, DateOnly PublicationDate, decimal Price);

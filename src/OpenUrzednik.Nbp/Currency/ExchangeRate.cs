namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Exchange rate
/// </summary>
/// <param name="TableId">Exchange rate table number</param>
/// <param name="PublicationDate">Publication date</param>
/// <param name="Price">Currency average exchange rate</param>
public sealed record ExchangeRate(string TableId, DateOnly PublicationDate, decimal Price);

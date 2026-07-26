namespace OpenUrzednik.Nbp.Currency;

/// <summary>
/// Exchange rate
/// </summary>
/// <param name="No">Exchange rate table number</param>
/// <param name="EffectiveDate">Publication date</param>
/// <param name="Mid">Currency average exchange rate</param>
public sealed record ExchangeRate(string No, DateOnly EffectiveDate, decimal Mid);

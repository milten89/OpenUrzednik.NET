using Bogus;

using OpenUrzednik.Nbp.Dto;

namespace OpenUrzednik.Nbp.Tests.Fakes;

internal sealed class BuySellExchangeRateDtoFaker : Faker<BuySellExchangeRateDto>
{
    public BuySellExchangeRateDtoFaker()
    {
        Bogus.DataSets.Currency currency = null!;

        RuleFor(x => x.CurrencyCode, f =>
        {
            currency = f.Finance.Currency();
            return currency.Code;
        });
        RuleFor(x => x.CurrencyName, f => currency.Description);
        RuleFor(x => x.Buy, f => f.Finance.Amount(1, 10));
        RuleFor(x => x.Sell, f => f.Finance.Amount(1, 10));
    }
}

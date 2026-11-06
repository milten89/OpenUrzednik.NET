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
        RuleFor(x => x.Bid, f => f.Finance.Amount(1, 10));
        RuleFor(x => x.Ask, (f, x) => x.Bid + f.Finance.Amount(0.01m, 0.5m)); // the API's ask is always above its bid
    }
}

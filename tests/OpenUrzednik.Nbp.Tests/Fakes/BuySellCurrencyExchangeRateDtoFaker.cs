using Bogus;

using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Tests.Extensions;

namespace OpenUrzednik.Nbp.Tests.Fakes;

internal sealed class BuySellCurrencyExchangeRateDtoFaker : Faker<BuySellCurrencyExchangeRateDto>
{
    public BuySellCurrencyExchangeRateDtoFaker()
    {
        RuleFor(x => x.TableId, f => f.Random.AlphaNumeric(10));
        RuleFor(x => x.PublicationDate, f => f.Date.AfterCurrencyMinDate());
        RuleFor(x => x.Bid, f => f.Finance.Amount(1, 10));
        RuleFor(x => x.Ask, (f, x) => x.Bid + f.Finance.Amount(0.01m, 0.5m)); // the API's ask is always above its bid
    }
}

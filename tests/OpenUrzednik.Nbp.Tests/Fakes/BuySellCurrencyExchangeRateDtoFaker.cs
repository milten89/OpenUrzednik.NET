using Bogus;

using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.TestCommon.Extensions;

namespace OpenUrzednik.Nbp.Tests.Fakes;

internal sealed class BuySellCurrencyExchangeRateDtoFaker : Faker<BuySellCurrencyExchangeRateDto>
{
    public BuySellCurrencyExchangeRateDtoFaker()
    {
        RuleFor(x => x.TableId, f => f.Random.AlphaNumeric(10));
        RuleFor(x => x.PublicationDate, f => f.Date.RecentDateOnly());
        RuleFor(x => x.Buy,  f => f.Finance.Amount(1, 10));
        RuleFor(x => x.Sell,  f => f.Finance.Amount(1, 10));
    }
}
using Bogus;

using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Tests.Extensions;
using OpenUrzednik.TestCommon.Extensions;

namespace OpenUrzednik.Nbp.Tests.Fakes;

internal sealed class BuySellExchangeRateTableDtoFaker : Faker<BuySellExchangeRateTableDto>
{
    private readonly BuySellExchangeRateDtoFaker _ratesFaker = new();

    public BuySellExchangeRateTableDtoFaker(int? ratesCount = null)
    {
        RuleFor(x => x.TableId, f => f.Random.AlphaNumeric(10));
        RuleFor(x => x.TradingDate, f => f.Date.AfterCurrencyMinDate());
        RuleFor(x => x.PublicationDate, f => f.Date.AfterCurrencyMinDate());
        RuleFor(x => x.Rates, f => [.. _ratesFaker.LinkRandomizerTo(this).Generate(ratesCount ?? f.Random.Int(3, 10))]);
    }
}

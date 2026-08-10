using Bogus;

using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Tests.Extensions;
using OpenUrzednik.TestCommon.Extensions;

namespace OpenUrzednik.Nbp.Tests.Fakes;

internal sealed class ExchangeRateTableDtoFaker : Faker<ExchangeRateTableDto>
{
    private readonly ExchangeRateDtoFaker _ratesFaker = new();

    public ExchangeRateTableDtoFaker(int? ratesCount = null)
    {
        RuleFor(x => x.TableId, f => f.Random.AlphaNumeric(10));
        RuleFor(x => x.PublicationDate, f => f.Date.AfterCurrencyMinDate());
        RuleFor(x => x.Rates, f => [.. _ratesFaker.LinkRandomizerTo(this).Generate(ratesCount ?? f.Random.Int(3, 10))]);
    }
}

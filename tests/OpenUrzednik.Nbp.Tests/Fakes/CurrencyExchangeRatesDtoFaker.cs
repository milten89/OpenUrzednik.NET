using Bogus;

using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.TestCommon.Extensions;

namespace OpenUrzednik.Nbp.Tests.Fakes;

internal sealed class CurrencyExchangeRatesDtoFaker : Faker<CurrencyExchangeRatesDto>
{
    private readonly CurrencyExchangeRateDtoFaker _ratesFaker = new();

    public CurrencyExchangeRatesDtoFaker(int? ratesCount = null)
    {
        Bogus.DataSets.Currency currency = null!;

        RuleFor(x => x.CurrencyCode, f =>
        {
            currency = f.Finance.Currency();
            return currency.Code;
        });
        RuleFor(x => x.CurrencyName, f => currency.Description);
        RuleFor(x => x.Rates, f => [.. _ratesFaker.LinkRandomizerTo(this).Generate(ratesCount ?? f.Random.Int(3, 10))]);
    }
}

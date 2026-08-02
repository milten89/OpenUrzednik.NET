using Bogus;
using OpenUrzednik.Nbp.Dto;

namespace OpenUrzednik.Nbp.Tests.Fakes;

internal sealed class CurrencyExchangeRateDtoFaker : Faker<CurrencyExchangeRateDto>
{
    public CurrencyExchangeRateDtoFaker()
    {
        RuleFor(x => x.TableId, f => f.Random.AlphaNumeric(10));
        RuleFor(x => x.PublicationDate, f => f.Date.RecentDateOnly());
        RuleFor(x => x.Price, f => f.Finance.Amount(1, 10));
    }
}
using Bogus;

using OpenUrzednik.Nbp.Dto;

namespace OpenUrzednik.Nbp.Tests.Fakes;

internal sealed class ExchangeRateDtoFaker : Faker<ExchangeRateDto>
{
    public ExchangeRateDtoFaker()
    {
        Bogus.DataSets.Currency currency = null!;

        RuleFor(x => x.CurrencyCode, f =>
        {
            currency = f.Finance.Currency();
            return currency.Code;
        });
        RuleFor(x => x.CurrencyName, f => currency.Description);
        RuleFor(x => x.Price, f => f.Finance.Amount(1, 10));
    }
}
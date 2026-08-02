using Bogus;

using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.TestCommon.Extensions;

namespace OpenUrzednik.Nbp.Tests.Fakes;

internal sealed class GoldPriceDtoFaker : Faker<GoldPriceDto>
{
    public GoldPriceDtoFaker()
    {
        RuleFor(x => x.Date, f => f.Date.RecentDateOnly());
        RuleFor(x => x.Price, f => f.Finance.Amount(1, 10));
    }
}
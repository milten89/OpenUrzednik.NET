using Bogus.DataSets;

using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Tests.Extensions;

public static class FakerExtensions
{
    public static DateOnly BeforeCurrencyMinDate(this Date date)
        => date.BetweenDateOnly(DateOnly.FromDateTime(TestCommon.Extensions.FakerExtensions.BetweenStart),
            CurrencyDateValidator.MinDate);

    public static DateOnly BeforeGoldMinDate(this Date date)
        => date.BetweenDateOnly(DateOnly.FromDateTime(TestCommon.Extensions.FakerExtensions.BetweenStart),
            GoldDateValidator.MinDate);

    public static DateOnly AfterCurrencyMinDate(this Date date)
        => date.BetweenDateOnly(CurrencyDateValidator.MinDate,
            DateOnly.FromDateTime(TestCommon.Extensions.FakerExtensions.BetweenEnd));

    public static DateOnly AfterGoldMinDate(this Date date)
        => date.BetweenDateOnly(GoldDateValidator.MinDate,
            DateOnly.FromDateTime(TestCommon.Extensions.FakerExtensions.BetweenEnd));
}

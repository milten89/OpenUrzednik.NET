using Bogus.DataSets;

using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Tests.Extensions;

public static class FakerExtensions
{
    // DateOnly is DateTime on .NET Framework (ADR-0005), where Bogus has no BetweenDateOnly.
    private static DateOnly Between(Date date, DateOnly from, DateOnly to)
#if NET
        => date.BetweenDateOnly(from, to);
#else
        => date.Between(from, to).Date;
#endif

    public static DateOnly BeforeCurrencyMinDate(this Date date)
        => Between(date, DateOnly.FromDateTime(TestCommon.Extensions.FakerExtensions.BetweenStart),
            CurrencyDateValidator.MinDate);

    public static DateOnly BeforeGoldMinDate(this Date date)
        => Between(date, DateOnly.FromDateTime(TestCommon.Extensions.FakerExtensions.BetweenStart),
            GoldDateValidator.MinDate);

    public static DateOnly AfterCurrencyMinDate(this Date date)
        => Between(date, CurrencyDateValidator.MinDate,
            DateOnly.FromDateTime(TestCommon.Extensions.FakerExtensions.BetweenEnd));

    public static DateOnly AfterGoldMinDate(this Date date)
        => Between(date, GoldDateValidator.MinDate,
            DateOnly.FromDateTime(TestCommon.Extensions.FakerExtensions.BetweenEnd));
}

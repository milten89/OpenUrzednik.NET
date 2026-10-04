using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;
using OpenUrzednik.Nbp.Table;

namespace OpenUrzednik.Nbp.Validation;

/// <summary>
/// Accepts <c>last/{n}</c> counts from 1 to <paramref name="max"/>; the NBP API rejects more with 400.
/// </summary>
internal sealed class TopCountValidator(string propertyName, int value, int max = TopCountValidator.MaxTopCount) : ValueValidator<int>(propertyName, value)
{
    /// <summary>The most results <c>exchangerates/rates</c> and <c>cenyzlota</c> return for <c>last/{n}</c>.</summary>
    public const int MaxTopCount = 255;

    /// <summary>
    /// The most tables <c>exchangerates/tables/a</c> and <c>/c</c> return for <c>last/{n}</c>. A fixed cap: the most business days
    /// a 93-day range can contain (13 weeks × 5 + 2).
    /// </summary>
    public const int MaxDailyTablesTopCount = 67;

    /// <summary>
    /// The most tables <c>exchangerates/tables/b</c> returns for <c>last/{n}</c>. A fixed cap: the most Wednesdays a 93-day range can contain.
    /// </summary>
    public const int MaxTableBTopCount = 14;

    public override string Name => "topCount";

    /// <summary>The <c>last/{n}</c> limit of the tables endpoint for <paramref name="table"/>.</summary>
    public static int MaxForTable(TableType table) => table == TableType.B ? MaxTableBTopCount : MaxDailyTablesTopCount;

    public override OpenUrzednikResult Validate()
    {
        return Value >= 1 && Value <= max
            ? OpenUrzednikResult.Success()
            : GetValidationErrorResult($"'{PropertyName}' should be between 1 and {max}.");
    }
}

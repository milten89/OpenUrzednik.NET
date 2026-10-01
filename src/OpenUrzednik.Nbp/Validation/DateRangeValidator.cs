using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;
using OpenUrzednik.Nbp.Extensions;

namespace OpenUrzednik.Nbp.Validation;

internal sealed class DateRangeValidator((DateOnly From, DateOnly To) value, int maxDays) : ValueValidator<(DateOnly From, DateOnly To)>("date range", value)
{
    /// <summary>Maximum <c>to - from</c> in days accepted by <c>exchangerates/rates</c> and <c>cenyzlota</c>.</summary>
    public const int MaxRatesDateRange = 367;

    /// <summary>Maximum <c>to - from</c> in days accepted by <c>exchangerates/tables</c>.</summary>
    public const int MaxTablesDateRange = 93;

    public override string Name => "dateRange";

    public override OpenUrzednikResult Validate()
    {
        if (Value.From > Value.To)
            return GetValidationErrorResult($"Start date '{Value.From.ToIso8601String()}' is greater than end date '{Value.To.ToIso8601String()}'.");

        if (Value.To.DayNumber - Value.From.DayNumber > maxDays)
            return GetValidationErrorResult($"Date range '{Value.From.ToIso8601String()} : {Value.To.ToIso8601String()}' should not exceed {maxDays} days.");

        return OpenUrzednikResult.Success();
    }
}

using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;
using OpenUrzednik.Nbp.Extensions;

namespace OpenUrzednik.Nbp.Validation;

internal sealed class DateRangeValidator((DateOnly From, DateOnly To) value) : ValueValidator<(DateOnly From, DateOnly To)>("date range", value)
{
    public const int MaxDateRange = 93;

    public override string Name => "dateRange";

    public override OpenUrzednikResult Validate()
    {
        if (Value.From > Value.To)
            return GetValidationErrorResult($"Start date '{Value.From.ToIso8601String()}' is greater than end date '{Value.To.ToIso8601String()}'.");

        if (Value.To.DayNumber - Value.From.DayNumber > MaxDateRange)
            return GetValidationErrorResult($"Date range '{Value.From.ToIso8601String()} : {Value.To.ToIso8601String()}' should be less than {MaxDateRange} days.");

        return OpenUrzednikResult.Success();
    }
}

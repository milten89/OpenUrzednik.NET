using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;

namespace OpenUrzednik.Nbp.Validation;

internal sealed class DateRangeValidator((DateOnly From, DateOnly To) value) : ValueValidator<(DateOnly From, DateOnly To)>("date range", value)
{
    private const int MaxDateRange = 93;

    public override string Name => "dateRange";

    public override OpenUrzednikResult Validate()
    {
        if (Value.From > Value.To)
            return GetValidationErrorResult($"Start date '{Value.From:d}' is greater than end date '{Value.To:d}'.");

        if (Value.To.DayNumber - Value.From.DayNumber > MaxDateRange)
            return GetValidationErrorResult($"Date range '{Value.From:d} : {Value.To:d}' should be less than {MaxDateRange} days.");

        return OpenUrzednikResult.Success();
    }
}

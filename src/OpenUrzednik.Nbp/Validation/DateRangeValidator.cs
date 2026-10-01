using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;
using OpenUrzednik.Nbp.Common;

namespace OpenUrzednik.Nbp.Validation;

internal sealed class DateRangeValidator((DateOnly From, DateOnly To) value) : ValueValidator<(DateOnly From, DateOnly To)>("date range", value)
{
    public const int MaxDateRange = 93;

    public override string Name => "dateRange";

    public override OpenUrzednikResult Validate()
    {
        if (Value.From > Value.To)
            return GetValidationErrorResult($"Start date '{NbpFormat.Date(Value.From)}' is greater than end date '{NbpFormat.Date(Value.To)}'.");

        if (Value.To.DayNumber - Value.From.DayNumber > MaxDateRange)
            return GetValidationErrorResult($"Date range '{NbpFormat.Date(Value.From)} : {NbpFormat.Date(Value.To)}' should be less than {MaxDateRange} days.");

        return OpenUrzednikResult.Success();
    }
}

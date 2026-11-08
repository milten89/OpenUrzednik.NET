using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Validation;
using OpenUrzednik.Nbp.Extensions;

namespace OpenUrzednik.Nbp.Validation;

// The error is a RangeValidationError<DateTime> naming both parameters, not a tuple value (ADR-0022).
internal sealed class DateRangeValidator(string startName, string endName, (DateOnly From, DateOnly To) value, int maxDays)
    : ValueValidator<(DateOnly From, DateOnly To)>(startName, value)
{
    /// <summary>Maximum <c>to - from</c> in days accepted by <c>exchangerates/rates</c> and <c>cenyzlota</c>.</summary>
    public const int MaxRatesDateRange = 367;

    /// <summary>Maximum <c>to - from</c> in days accepted by <c>exchangerates/tables</c>.</summary>
    public const int MaxTablesDateRange = 93;

    public override string Name => "dateRange";

    public override OpenUrzednikResult Validate()
    {
        if (Value.From.DayNumber > Value.To.DayNumber)
            return Failure($"Start date '{Value.From.ToIso8601String()}' is greater than end date '{Value.To.ToIso8601String()}'.");

        if (Value.To.DayNumber - Value.From.DayNumber > maxDays)
            return Failure($"Date range '{Value.From.ToIso8601String()} : {Value.To.ToIso8601String()}' should not exceed {maxDays} days.");

        return OpenUrzednikResult.Success();
    }

    private OpenUrzednikResult Failure(string message)
        => OpenUrzednikResult.Failure(new RangeValidationError<DateTime>(message, Name, PropertyName, Value.From.ToPublicDate(), endName, Value.To.ToPublicDate()));
}

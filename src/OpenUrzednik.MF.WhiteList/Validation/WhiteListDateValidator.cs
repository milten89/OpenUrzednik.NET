using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Validation;

namespace OpenUrzednik.MF.WhiteList.Validation;

/// <summary>
/// Accepts the dates the list covers (art. 96b ust. 2 of the VAT act, ADR-0023): from 1 January of the fifth year before
/// <paramref name="today"/> to <paramref name="today"/>, both by the Warsaw calendar. The API rejects a future date (<c>WL-103</c>) and an
/// earlier one (<c>WL-118</c>); checking first spends no quota. The error is a <see cref="ValidationError{T}"/> of <see cref="DateTime"/>
/// (ADR-0005), never of the internal <c>DateOnly</c>.
/// </summary>
internal sealed class WhiteListDateValidator(string propertyName, DateOnly value, DateOnly today) : ValueValidator<DateOnly>(propertyName, value)
{
    /// <summary>How many years before the current one the list reaches back, to its 1 January.</summary>
    internal const int YearsBack = 5;

    public override string Name => "whiteListDate";

    /// <summary>The earliest date the list covers when it is <paramref name="today"/> in Warsaw.</summary>
    internal static DateOnly MinDate(DateOnly today) => new(today.Year - YearsBack, 1, 1);

    public override OpenUrzednikResult Validate()
    {
        var minDate = MinDate(today);

        // DayNumber ignores the time of day a DateTime can carry on netstandard2.0.
        if (Value.DayNumber < minDate.DayNumber)
            return Failure($"'{PropertyName}' should not be earlier than {minDate.ToIso8601String()} (1 January {YearsBack} years back, Europe/Warsaw): the list doesn't cover earlier dates.");

        if (Value.DayNumber > today.DayNumber)
            return Failure($"'{PropertyName}' should not be later than today ({today.ToIso8601String()}, Europe/Warsaw).");

        return OpenUrzednikResult.Success();
    }

    private OpenUrzednikResult Failure(string message)
        => OpenUrzednikResult.Failure(new ValidationError<DateTime>(message, Name, PropertyName, Value.ToPublicDate()));
}

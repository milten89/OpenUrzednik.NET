using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Table;

namespace OpenUrzednik.Nbp.Validation;

/// <summary>
/// Checks the date of a "last published before" request. The request is a range that ends the day before <paramref name="value"/>,
/// and that end must be a date the NBP API accepts: from <paramref name="minDate"/> to <paramref name="today"/> (the current date
/// in Europe/Warsaw). So the date itself runs from the day after <paramref name="minDate"/> to tomorrow. The error names the date
/// the caller passed, not the end of the range.
/// </summary>
internal sealed class LastPublishedBeforeDateValidator(string propertyName, DateOnly value, DateOnly minDate, DateOnly today) : ValueValidator<DateOnly>(propertyName, value)
{
    /// <summary>
    /// Days the range reaches back for daily publications (tables A and C, gold): longer than any gap between two
    /// publications (a weekend plus the longest run of holidays).
    /// </summary>
    public const int DailyWindowDays = 14;

    /// <summary>Days the range reaches back for table B, published once a week.</summary>
    public const int WeeklyWindowDays = 28;

    public override string Name => "lastPublishedBeforeDate";

    /// <summary>The window for the mid rate <paramref name="table"/>.</summary>
    public static int WindowForTable(TableType table) => table == TableType.B ? WeeklyWindowDays : DailyWindowDays;

    public override OpenUrzednikResult Validate()
    {
        // DayNumber ignores the time of day a DateTime can carry on netstandard2.0.
        if (Value.DayNumber <= minDate.DayNumber)
            return GetValidationErrorResult($"'{PropertyName}' should be later than {minDate.ToIso8601String()}.");

        if (Value.DayNumber > today.DayNumber + 1)
            return GetValidationErrorResult($"'{PropertyName}' should not be later than tomorrow ({today.AddDays(1).ToIso8601String()}, Europe/Warsaw).");

        return OpenUrzednikResult.Success();
    }
}

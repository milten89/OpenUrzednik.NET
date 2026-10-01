using OpenUrzednik.Core;
using OpenUrzednik.Core.Validation;
using OpenUrzednik.Nbp.Extensions;

namespace OpenUrzednik.Nbp.Validation;

/// <summary>
/// Accepts dates from <see cref="MinDate"/> to <paramref name="today"/> (the current date in Europe/Warsaw);
/// the NBP API rejects future dates with 400.
/// </summary>
internal sealed class GoldDateValidator(string propertyName, DateOnly value, DateOnly today) : ValueValidator<DateOnly>(propertyName, value)
{
    public static readonly DateOnly MinDate = new(2013, 1, 2);

    public override string Name => "goldDate";

    public override OpenUrzednikResult Validate()
    {
        if (Value < MinDate)
            return GetValidationErrorResult($"'{PropertyName}' should be greater or equal {MinDate.ToIso8601String()}.");

        if (Value > today)
            return GetValidationErrorResult($"'{PropertyName}' should not be later than today ({today.ToIso8601String()}, Europe/Warsaw).");

        return OpenUrzednikResult.Success();
    }
}

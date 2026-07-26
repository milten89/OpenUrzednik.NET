using System.Text.RegularExpressions;
using OpenUrzednik.Core.Validation;

namespace OpenUrzednik.Nbp.Validation;

internal static partial class ValidationExtensions
{
    private static readonly DateOnly MinCurrencyDate = new(2002, 1, 2);
    private static readonly DateOnly MinGoldDate = new(2013, 1, 2);
    private const int MaxDateRange = 93;

    [GeneratedRegex(@"^[A-Z]{3}$")]
    private static partial Regex Iso4217CurrencyCodeRegex();

    internal static ValueValidator<DateOnly> EnsureGreaterThanMinCurrencyDate(this ValueValidator<DateOnly> validator)
    {
        ArgumentNullException.ThrowIfNull(validator, nameof(validator));

        if (validator.Value < MinCurrencyDate)
            validator.AddValidationError($"{validator.Name ?? validator.Value.ToString()} should be greater or equal {MinCurrencyDate:d}");

        return validator;
    }

    internal static ValueValidator<DateOnly> EnsureGreaterThanMinGoldDate(this ValueValidator<DateOnly> validator)
    {
        ArgumentNullException.ThrowIfNull(validator, nameof(validator));

        if (validator.Value < MinGoldDate)
            validator.AddValidationError($"{validator.Name ?? validator.Value.ToString()} should be greater or equal {MinGoldDate:d}");

        return validator;
    }

    internal static ValueValidator<(DateOnly From, DateOnly To)> EnsureValidRange(this ValueValidator<(DateOnly From, DateOnly To)> validator)
    {
        ArgumentNullException.ThrowIfNull(validator, nameof(validator));

        if (validator.Value.From > validator.Value.To)
            validator.AddValidationError($"Invalid date range. Start date ({validator.Value.From:d}) is greater than end date ({validator.Value.To:d}).");

        return validator;
    }

    internal static ValueValidator<(DateOnly From, DateOnly To)> EnsureRangeSize(this ValueValidator<(DateOnly From, DateOnly To)> validator)
    {
        ArgumentNullException.ThrowIfNull(validator, nameof(validator));

        if (validator.Value.To.DayNumber - validator.Value.From.DayNumber > MaxDateRange)
            validator.AddValidationError($"Date range ({validator.Value.From:d} : {validator.Value.To:d}) should be less than {MaxDateRange} days");

        return validator;
    }

    internal static ValueValidator<string> EnsureIso4217Code(this ValueValidator<string> validator)
    {
        ArgumentNullException.ThrowIfNull(validator, nameof(validator));

        if (!Iso4217CurrencyCodeRegex().IsMatch(validator.Value))
            validator.AddValidationError($"'{validator.Value}' is not proper ISO 4217 currency code");

        return validator;
    }

    internal static ValueValidator<int> EnsureProperTopCount(this ValueValidator<int> validator)
    {
        ArgumentNullException.ThrowIfNull(validator, nameof(validator));

        if (validator.Value <= 0)
            validator.AddValidationError($"{validator.Name ?? validator.Value.ToString()} should be greater or equal 1");

        return validator;
    }
}
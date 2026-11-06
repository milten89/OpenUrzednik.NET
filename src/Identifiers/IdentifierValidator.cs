using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Validation;
using OpenUrzednik.Privacy;

namespace OpenUrzednik.Identifiers;

/// <summary>
/// Base of the identifier validators: normalizes the value (<see cref="IdentifierNormalizer"/>), checks that the rest is digits of an
/// allowed length, then the identifier's own rules. Errors never contain the value: the message names the parameter, and the error's
/// value metadata is masked (<see cref="SensitiveValue.Mask"/>; ADR-0002, ADR-0003).
/// </summary>
internal abstract class IdentifierValidator : ValueValidator<string>
{
    /// <summary>Initializes the validator and normalizes <paramref name="value"/>.</summary>
    /// <param name="propertyName">Name of the validated parameter.</param>
    /// <param name="value">The identifier as given.</param>
    /// <param name="allowCountryPrefix">Whether a leading <c>PL</c> is accepted and removed.</param>
    /// <remarks>The client throws <see cref="ArgumentNullException"/> for <see langword="null"/> before it validates (ADR-0002); a <see langword="null"/>
    /// that gets here anyway is reported as empty.</remarks>
    protected IdentifierValidator(string propertyName, string value, bool allowCountryPrefix)
        : base(propertyName, value)
        => Normalized = value is null ? string.Empty : IdentifierNormalizer.Normalize(value, allowCountryPrefix);

    /// <summary>Gets the identifier without separators and prefix: the form to send to the API once <see cref="Validate"/> succeeds.</summary>
    internal string Normalized { get; }

    /// <summary>Gets what a valid value looks like, for the error message (e.g. <c>a NIP of 10 digits</c>).</summary>
    protected abstract string Format { get; }

    /// <inheritdoc />
    public sealed override OpenUrzednikResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Value))
            return Failure($"'{PropertyName}' is empty or whitespace.");

        var digits = Normalized;
        if (!HasValidLength(digits.Length) || !AreDigits(digits))
            return Failure($"'{PropertyName}' must be {Format}.");

        return ValidateDigits(digits);
    }

    /// <summary>Whether <paramref name="length"/> digits is an allowed length.</summary>
    protected abstract bool HasValidLength(int length);

    /// <summary>Checks the identifier's own rules on its digits, of an allowed length.</summary>
    protected abstract OpenUrzednikResult ValidateDigits(string digits);

    /// <summary>A failure for this rule and parameter, with the value masked.</summary>
    protected OpenUrzednikResult Failure(string message)
        => OpenUrzednikResult.Failure(new ValidationError(message, Name, PropertyName, SensitiveValue.Mask(Value)));

    /// <summary>A failure for a wrong check digit.</summary>
    protected OpenUrzednikResult InvalidCheckDigit() => Failure($"'{PropertyName}' has an invalid check digit.");

    /// <summary>A failure for a number that passes the checks but is never issued (all zeros).</summary>
    protected OpenUrzednikResult NotIssued() => Failure($"'{PropertyName}' isn't an issued number.");

    /// <summary>The value of the digit at <paramref name="index"/>.</summary>
    protected static int Digit(string digits, int index) => digits[index] - '0';

    /// <summary>Whether the first <paramref name="count"/> digits are all zeros.</summary>
    protected static bool AreZeros(string digits, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (digits[i] != '0')
                return false;
        }

        return true;
    }

    /// <summary>The weighted sum of the digits from <paramref name="start"/>, one weight per digit.</summary>
    protected static int WeightedSum(string digits, int start, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
            sum += Digit(digits, start + i) * weights[i];
        return sum;
    }

    private static bool AreDigits(string value)
    {
        foreach (var c in value)
        {
            if (c is < '0' or > '9')
                return false;
        }

        return true;
    }
}

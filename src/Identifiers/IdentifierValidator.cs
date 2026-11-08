#if NET
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
#endif

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Validation;
using OpenUrzednik.Privacy;

namespace OpenUrzednik.Identifiers;

/// <summary>
/// Base of the identifier validators: removes separators (<see cref="IdentifierNormalizer"/>) and the identifier's country prefix, if it
/// accepts one, checks that the rest is digits of an allowed length, then the identifier's own rules. Errors never contain the value:
/// the message names the parameter, and the error's value metadata is masked (<see cref="SensitiveValue.Mask"/>; ADR-0002, ADR-0003).
/// </summary>
internal abstract class IdentifierValidator : ValueValidator<string>
{
    /// <summary>Initializes the validator and normalizes <paramref name="value"/>.</summary>
    /// <param name="propertyName">Name of the validated parameter.</param>
    /// <param name="value">The identifier as given.</param>
    /// <param name="countryPrefix">
    /// The country prefix the identifier may carry, removed when present (any case): <c>PL</c> for a NIP in the EU VAT form and an account
    /// number in the IBAN form; <see langword="null"/> when it takes none. Any other country's prefix is rejected, never removed: a foreign
    /// number could otherwise pass as a Polish one.
    /// </param>
    /// <remarks>The client throws <see cref="ArgumentNullException"/> for <see langword="null"/> before it validates (ADR-0002); a <see langword="null"/>
    /// that gets here anyway is reported as empty.</remarks>
    protected IdentifierValidator(string propertyName, string value, string? countryPrefix)
        : base(propertyName, value)
    {
        CountryPrefix = countryPrefix;
        if (value is null)
        {
            Normalized = string.Empty;
            return;
        }

        var hasPrefix = countryPrefix is not null && IdentifierNormalizer.StartsWith(value, countryPrefix);
        Normalized = IdentifierNormalizer.Normalize(value, hasPrefix ? countryPrefix!.Length : 0);
    }

    /// <summary>Gets the identifier without separators and prefix: the form to send to the API once <see cref="Validate"/> succeeds.</summary>
    internal string Normalized { get; }

    /// <summary>Gets the country prefix the identifier accepts, or <see langword="null"/>.</summary>
    protected string? CountryPrefix { get; }

    /// <summary>Gets what a valid value looks like, for the error message (e.g. <c>a NIP of 10 digits</c>).</summary>
    protected abstract string Format { get; }

    /// <inheritdoc />
    public sealed override OpenUrzednikResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Value))
            return Failure($"'{PropertyName}' is empty or whitespace.");

        var digits = Normalized;
        if (!HasValidLength(digits.Length) || !AreDigits(digits))
        {
            // Two letters left where the accepted prefix would be: another country's prefix (DE…, BE…).
            if (CountryPrefix is not null && digits.Length >= 2 && IsAsciiLetter(digits[0]) && IsAsciiLetter(digits[1]))
                return Failure($"'{PropertyName}' must be a Polish number: the only accepted country prefix is {CountryPrefix}.");
            return Failure($"'{PropertyName}' must be {Format}.");
        }

        return ValidateDigits(digits);
    }

    /// <summary>Whether <paramref name="length"/> digits is an allowed length.</summary>
    protected abstract bool HasValidLength(int length);

    /// <summary>Checks the identifier's own rules on its digits, of an allowed length.</summary>
    protected abstract OpenUrzednikResult ValidateDigits(string digits);

    /// <summary>A failure for this rule and parameter, with the value masked.</summary>
    protected OpenUrzednikResult Failure(string message)
        => OpenUrzednikResult.Failure(new ValidationError<string>(message, Name, PropertyName, SensitiveValue.Mask(Value)));

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
    /// <remarks>
    /// On .NET, 8 digits at a time with <c>Vector128</c> where the hardware supports it: 1.6 to 3 times faster than the loop for
    /// the 8 to 13 weights used here (BenchmarkDotNet, .NET 10, x64). A bounds-checked span version was slower than the loop for a NIP,
    /// hence the unchecked loads, kept inside <paramref name="digits"/> by the range check. netstandard2.0 keeps the loop.
    /// </remarks>
    protected static int WeightedSum(string digits, int start, int[] weights)
    {
        var sum = 0;
        var i = 0;
#if NET
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start + weights.Length, digits.Length, nameof(weights));
        // Stryker disable once Logical,Boolean,Equality : equivalent. This condition and the vector loop's bound only decide how many digits the vector part sums; the loop after #endif sums the rest, so the result is the same (performance only).
        if (Vector128.IsHardwareAccelerated && weights.Length >= Vector128<ushort>.Count)
        {
            ref var chars = ref Unsafe.As<char, ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(digits.AsSpan()), start));
            ref var weight = ref Unsafe.As<int, uint>(ref MemoryMarshal.GetArrayDataReference(weights));
            var zero = Vector128.Create((ushort)'0');
            var total = Vector128<uint>.Zero;
            for (; i + Vector128<ushort>.Count <= weights.Length; i += Vector128<ushort>.Count)
            {
                // 8 digits as ushort, widened to two vectors of 4 uints, times their 4 weights each.
                var (low, high) = Vector128.Widen(Vector128.LoadUnsafe(ref chars, (nuint)i) - zero);
                total += low * Vector128.LoadUnsafe(ref weight, (nuint)i) + high * Vector128.LoadUnsafe(ref weight, (nuint)i + 4);
            }

            sum = (int)Vector128.Sum(total);
        }
#endif
        for (; i < weights.Length; i++)
            sum += Digit(digits, start + i) * weights[i];
        return sum;
    }

    private static bool IsAsciiLetter(char c) => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');

    // On .NET the vectorized span search: 8 times faster than the loop for 26 digits (BenchmarkDotNet, .NET 10, x64).
    // char.IsAsciiDigit is no faster than the range check below. AreZeros stays a loop: a real number stops it at the first digit.
    private static bool AreDigits(string value)
    {
#if NET
        return !value.AsSpan().ContainsAnyExceptInRange('0', '9');
#else
        foreach (var c in value)
        {
            if (c is < '0' or > '9')
                return false;
        }

        return true;
#endif
    }
}

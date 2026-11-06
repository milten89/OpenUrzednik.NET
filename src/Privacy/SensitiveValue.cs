using System.Diagnostics.CodeAnalysis;

namespace OpenUrzednik.Privacy;

/// <summary>
/// Masks personal identifiers (PESEL, NIP, REGON, bank account numbers) and secrets before they reach log messages or
/// properties, span tags, error and exception messages, error metadata or <c>Exception.Data</c> (ADR-0003).
/// </summary>
internal static class SensitiveValue
{
    /// <summary>How many trailing characters <see cref="Mask"/> leaves visible.</summary>
    internal const int VisibleCharacters = 4;

    /// <summary>The character that replaces the hidden characters.</summary>
    internal const char MaskCharacter = '*';

    /// <summary>
    /// Replaces every character except the last <see cref="VisibleCharacters"/> with <see cref="MaskCharacter"/>, keeping the length
    /// (<c>1234567890</c> becomes <c>******7890</c>). A value of <see cref="VisibleCharacters"/> characters or fewer is masked entirely,
    /// so it is never shown in full.
    /// </summary>
    /// <param name="value">The value to mask.</param>
    /// <returns>The masked value; <see langword="null"/> for <see langword="null"/>, and an empty string for an empty one.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    internal static string? Mask(string? value)
    {
        if (value is null || value.Length == 0)
        {
            return value;
        }

#if NET
        return string.Create(value.Length, value, static (span, source) =>
        {
            int hidden = source.Length > VisibleCharacters ? source.Length - VisibleCharacters : source.Length;
            span[..hidden].Fill(MaskCharacter);
            source.AsSpan(hidden).CopyTo(span[hidden..]);
        });
#else
        // netstandard2.0 has no string.Create with a span, and a provider there may not reference System.Memory.
        int hidden = value.Length > VisibleCharacters ? value.Length - VisibleCharacters : value.Length;
        char[] chars = new char[value.Length];
        for (int i = 0; i < hidden; i++)
        {
            chars[i] = MaskCharacter;
        }

        value.CopyTo(hidden, chars, hidden, value.Length - hidden);
        return new string(chars);
#endif
    }
}

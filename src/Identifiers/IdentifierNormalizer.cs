namespace OpenUrzednik.Identifiers;

/// <summary>
/// Turns an identifier as people write it (<c>123-456-32-18</c>, <c>PL61 1090 1014 …</c>) into the bare characters APIs take:
/// white space (<see cref="char.IsWhiteSpace(char)"/>, so also non-breaking spaces) and ASCII hyphens are removed, and, where allowed,
/// a leading <c>PL</c> country prefix (any case). Other dashes (en dash, non-breaking hyphen) stay, so the validators reject them.
/// </summary>
internal static class IdentifierNormalizer
{
    /// <summary>Normalizes <paramref name="value"/>; returns it unchanged, without allocating, when there is nothing to remove.</summary>
    /// <param name="value">The identifier as given.</param>
    /// <param name="allowCountryPrefix">Whether a leading <c>PL</c> (NIP in the EU VAT form, an account number in the IBAN form) is removed.</param>
    internal static string Normalize(string value, bool allowCountryPrefix)
    {
        var start = 0;
        while (start < value.Length && IsSeparator(value[start]))
            start++;

        if (allowCountryPrefix && start + 1 < value.Length && (value[start] | 0x20) == 'p' && (value[start + 1] | 0x20) == 'l')
            start += 2;

        var length = 0;
        for (var i = start; i < value.Length; i++)
        {
            if (!IsSeparator(value[i]))
                length++;
        }

        if (length == value.Length)
            return value;

#if NET
        return string.Create(length, (value, start), static (span, state) =>
        {
            var position = 0;
            for (var i = state.start; i < state.value.Length; i++)
            {
                if (!IsSeparator(state.value[i]))
                    span[position++] = state.value[i];
            }
        });
#else
        var chars = new char[length];
        var position = 0;
        for (var i = start; i < value.Length; i++)
        {
            if (!IsSeparator(value[i]))
                chars[position++] = value[i];
        }

        return new string(chars);
#endif
    }

    private static bool IsSeparator(char c) => c == '-' || char.IsWhiteSpace(c);
}

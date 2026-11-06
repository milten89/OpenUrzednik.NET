namespace OpenUrzednik.Identifiers;

/// <summary>
/// Removes the separators people write in identifiers (<c>123-456-32-18</c>, <c>61 1090 1014 …</c>): white space
/// (<see cref="char.IsWhiteSpace(char)"/>, so also non-breaking spaces) and ASCII hyphens. Other dashes (en dash, non-breaking hyphen)
/// stay, so the validators reject them. It knows nothing about identifiers: which prefix an identifier accepts is the validator's rule.
/// </summary>
internal static class IdentifierNormalizer
{
    /// <summary>
    /// Whether the characters of <paramref name="value"/> that aren't separators start with <paramref name="prefix"/>, ignoring case.
    /// </summary>
    internal static bool StartsWith(string value, string prefix)
    {
        var matched = 0;
        for (var i = 0; i < value.Length && matched < prefix.Length; i++)
        {
            if (IsSeparator(value[i]))
                continue;
            if (char.ToUpperInvariant(value[i]) != char.ToUpperInvariant(prefix[matched]))
                return false;
            matched++;
        }

        return matched == prefix.Length;
    }

    /// <summary>
    /// Removes the separators from <paramref name="value"/>, then its first <paramref name="skip"/> remaining characters (a prefix the caller
    /// found with <see cref="StartsWith"/>). Returns <paramref name="value"/> itself, without allocating, when there is nothing to remove.
    /// </summary>
    internal static string Normalize(string value, int skip = 0)
    {
        var kept = 0;
        foreach (var c in value)
        {
            if (!IsSeparator(c))
                kept++;
        }

        if (kept == value.Length && skip == 0)
            return value;

        var length = Math.Max(kept - skip, 0);
#if NET
        return string.Create(length, (value, skip), static (span, state) =>
        {
            var toSkip = state.skip;
            var position = 0;
            foreach (var c in state.value)
            {
                if (IsSeparator(c))
                    continue;
                if (toSkip > 0)
                    toSkip--;
                else
                    span[position++] = c;
            }
        });
#else
        var chars = new char[length];
        var toSkip = skip;
        var position = 0;
        foreach (var c in value)
        {
            if (IsSeparator(c))
                continue;
            if (toSkip > 0)
                toSkip--;
            else
                chars[position++] = c;
        }

        return new string(chars);
#endif
    }

    private static bool IsSeparator(char c) => c == '-' || char.IsWhiteSpace(c);
}

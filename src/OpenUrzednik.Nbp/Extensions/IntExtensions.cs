using System.Globalization;

namespace OpenUrzednik.Nbp.Extensions;

internal static class IntExtensions
{
    /// <summary>
    /// Formats the number with invariant culture: ASCII digits and an ASCII minus sign
    /// (some cultures, e.g. fa-IR, use U+2212 instead).
    /// </summary>
    internal static string ToInvariantString(this int value)
        => value.ToString(CultureInfo.InvariantCulture);
}

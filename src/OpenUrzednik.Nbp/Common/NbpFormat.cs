using System.Globalization;

namespace OpenUrzednik.Nbp.Common;

/// <summary>
/// Culture-independent formatting for URLs and messages. The current culture must not leak in:
/// a non-Gregorian calendar (e.g. th-TH) would turn 2026 into 2569.
/// </summary>
internal static class NbpFormat
{
    internal static string Date(DateOnly date)
        => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    internal static string Integer(int value)
        => value.ToString(CultureInfo.InvariantCulture);
}

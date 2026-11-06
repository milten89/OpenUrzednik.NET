using OpenUrzednik.Nbp.Table;

namespace OpenUrzednik.Nbp.Extensions;

internal static class TableTypeExtensions
{
    /// <summary>
    /// The <c>nbp.table</c> span tag value: a string literal, because tag values should be primitives (OpenTelemetry) and
    /// a boxed enum would reach in-process listeners as <see cref="TableType"/>. The callers validate the value first (ADR-0002).
    /// </summary>
    internal static string ToTagValue(this TableType table) => table switch
    {
        TableType.A => "A",
        TableType.B => "B",
        _ => "unknown",
    };
}

namespace OpenUrzednik.MF.WhiteList;

/// <summary>
/// Names for connecting the MF White List clients to logging and tracing (ADR-0003).
/// </summary>
public static class WhiteListTelemetry
{
    /// <summary>
    /// The <c>ActivitySource</c> name for MF White List spans, e.g. for <c>AddSource</c> in OpenTelemetry or <c>ActivityTraceSource.GetShared</c>
    /// in <c>OpenUrzednik.Diagnostics</c>. On .NET 8 and later a client created without a trace source records its spans on this source.
    /// It is also the logger category prefix: <c>CreateOpenUrzednikLogger&lt;TClient&gt;</c> in <c>OpenUrzednik.Extensions.Logging</c>
    /// uses the client's full type name, which starts with it. Span names start with <c>mf_white_list.</c>.
    /// </summary>
    public const string SourceName = "OpenUrzednik.MF.WhiteList";
}

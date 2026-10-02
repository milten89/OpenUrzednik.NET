namespace OpenUrzednik.Nbp;

/// <summary>
/// Names for connecting the NBP clients to logging and tracing (ADR-0003).
/// </summary>
public static class NbpTelemetry
{
    /// <summary>
    /// The <c>ActivitySource</c> name for NBP spans, e.g. for <c>ActivityTraceSource.GetShared</c> in <c>OpenUrzednik.Diagnostics</c>.
    /// It is also the logger category prefix: <c>CreateOpenUrzednikLogger&lt;TClient&gt;</c> in <c>OpenUrzednik.Extensions.Logging</c>
    /// uses the client's full type name, which starts with it.
    /// </summary>
    public const string SourceName = "OpenUrzednik.Nbp";
}

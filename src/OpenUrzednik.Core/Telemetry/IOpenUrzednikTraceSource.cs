namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// Starts the spans the OpenUrzednik.NET clients record, so the core packages don't depend on a tracing library (ADR-0003).
/// Use <see cref="NullOpenUrzednikTraceSource"/> to record nothing, or the adapter in <c>OpenUrzednik.Diagnostics</c> for <c>ActivitySource</c>.
/// </summary>
public interface IOpenUrzednikTraceSource
{
    /// <summary>
    /// Starts a span, as a child of the current one if there is one. Dispose it to end it.
    /// </summary>
    /// <param name="operationName">The span name, <c>&lt;provider&gt;.&lt;area&gt;.&lt;operation&gt;</c> (e.g. <c>nbp.gold.latest</c>).</param>
    /// <returns>The span; a no-op span when nothing records it.</returns>
    IOpenUrzednikSpan StartSpan(string operationName);
}

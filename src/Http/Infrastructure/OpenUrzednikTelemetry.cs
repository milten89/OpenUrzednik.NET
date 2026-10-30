using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Http.Infrastructure;

/// <summary>
/// The logger and trace source a provider client uses, with defaults (ADR-0003).
/// For provider authors; applications pass <see cref="IOpenUrzednikLogger"/> and <see cref="IOpenUrzednikTraceSource"/> to the clients.
/// </summary>
/// <param name="logger">The logger, or <see langword="null"/> for <see cref="NullOpenUrzednikLogger"/>.</param>
/// <param name="traceSource">
/// The trace source, or <see langword="null"/> for the default: on the .NET targets the process-wide <c>ActivitySource</c> named
/// <paramref name="sourceName"/>, on netstandard2.0 (or without a name) <see cref="NullOpenUrzednikTraceSource"/>.
/// </param>
/// <param name="sourceName">The provider's <c>ActivitySource</c> name, e.g. <c>OpenUrzednik.Nbp</c>.</param>
internal sealed class OpenUrzednikTelemetry(IOpenUrzednikLogger? logger = null, IOpenUrzednikTraceSource? traceSource = null, string? sourceName = null)
{
    /// <summary>
    /// Gets the logger. Never <see langword="null"/>.
    /// </summary>
    public IOpenUrzednikLogger Logger { get; } = logger ?? NullOpenUrzednikLogger.Instance;

    /// <summary>
    /// Gets the trace source. Never <see langword="null"/>.
    /// </summary>
    public IOpenUrzednikTraceSource TraceSource { get; } = traceSource ?? DefaultTraceSource(sourceName);

    // netstandard2.0 has no ActivitySource without the System.Diagnostics.DiagnosticSource package, which Core and providers don't take.
    private static IOpenUrzednikTraceSource DefaultTraceSource(string? sourceName)
#if NET
        => sourceName is null ? NullOpenUrzednikTraceSource.Instance : SharedActivityTraceSource.Get(sourceName);
#else
        => NullOpenUrzednikTraceSource.Instance;
#endif
}

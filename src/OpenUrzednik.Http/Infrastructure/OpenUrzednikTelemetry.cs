using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Http.Infrastructure;

/// <summary>
/// The logger and trace source a provider client uses, with no-op defaults (ADR-0003).
/// For provider authors; applications pass <see cref="IOpenUrzednikLogger"/> and <see cref="IOpenUrzednikTraceSource"/> to the clients.
/// </summary>
/// <param name="logger">The logger, or <see langword="null"/> for <see cref="NullOpenUrzednikLogger"/>.</param>
/// <param name="traceSource">The trace source, or <see langword="null"/> for <see cref="NullOpenUrzednikTraceSource"/>.</param>
public sealed class OpenUrzednikTelemetry(IOpenUrzednikLogger? logger = null, IOpenUrzednikTraceSource? traceSource = null)
{
    /// <summary>
    /// Gets the logger. Never <see langword="null"/>.
    /// </summary>
    public IOpenUrzednikLogger Logger { get; } = logger ?? NullOpenUrzednikLogger.Instance;

    /// <summary>
    /// Gets the trace source. Never <see langword="null"/>.
    /// </summary>
    public IOpenUrzednikTraceSource TraceSource { get; } = traceSource ?? NullOpenUrzednikTraceSource.Instance;
}

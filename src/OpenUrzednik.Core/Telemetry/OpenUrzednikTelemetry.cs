namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// The logger, trace source and metrics a client uses, passed to its constructor as one value (ADR-0025). A property left
/// <see langword="null"/> takes that kind's default.
/// </summary>
/// <remarks>
/// A client reads the properties once, in its constructor, so changing them afterwards doesn't affect it. One instance may be
/// shared by clients of several providers: the defaults use each client's own provider names.
/// </remarks>
public sealed class OpenUrzednikTelemetry
{
    /// <summary>
    /// Gets or sets the logger, or <see langword="null"/> (the default) to log nothing.
    /// </summary>
    public IOpenUrzednikLogger? Logger { get; set; }

    /// <summary>
    /// Gets or sets the trace source, or <see langword="null"/> (the default) for the provider's shared <c>ActivitySource</c>
    /// (<c>OpenUrzednik.&lt;Provider&gt;</c>) on .NET 8 and later, and no spans on .NET Framework (netstandard2.0).
    /// </summary>
    public IOpenUrzednikTraceSource? TraceSource { get; set; }

    /// <summary>
    /// Gets or sets the metrics, or <see langword="null"/> (the default) for the provider's shared <c>Meter</c>
    /// (<c>OpenUrzednik.&lt;Provider&gt;</c>) on .NET 8 and later, and no metrics on .NET Framework (netstandard2.0).
    /// </summary>
    public IOpenUrzednikMetrics? Metrics { get; set; }
}

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Diagnostics;

/// <summary>
/// An <see cref="IOpenUrzednikMetrics"/> that records the clients' measurements on a <see cref="System.Diagnostics.Metrics.Meter"/>
/// named after the provider (<c>OpenUrzednik.&lt;Provider&gt;</c>, e.g. <c>NbpTelemetry.SourceName</c>), so OpenTelemetry
/// and other listeners subscribe with <c>AddMeter("OpenUrzednik.*")</c>. Each instrument is created from the first
/// measurement recorded with its name; measurements of a kind this version doesn't know are ignored.
/// </summary>
/// <remarks>
/// There is one meter per name for the process, shared with the clients' own default on .NET 8 and later, so get an
/// instance with <see cref="GetShared"/>. Histogram bucket advice (<c>InstrumentAdvice</c>) is applied on .NET 10 and
/// .NET Framework, not on .NET 8, whose built-in System.Diagnostics.DiagnosticSource predates it.
/// </remarks>
public sealed class MeterMetrics : IOpenUrzednikMetrics
{
    // Lazy, so a race in GetOrAdd can't create a second instance for one name.
    private static readonly ConcurrentDictionary<string, Lazy<MeterMetrics>> Shared = new(StringComparer.Ordinal);

    private readonly SharedOpenUrzednikMeter _meter;

    private MeterMetrics(SharedOpenUrzednikMeter meter) => _meter = meter;

    /// <summary>The meter the measurements are recorded on.</summary>
    public Meter Meter => _meter.Meter;

    /// <summary>
    /// Returns the metrics for the meter <paramref name="name"/>, shared by the whole process and with the clients' own
    /// default on .NET 8 and later.
    /// </summary>
    /// <param name="name">The meter name, the provider's source name (e.g. <c>NbpTelemetry.SourceName</c>).</param>
    /// <returns>The same instance for the same name.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or white space.</exception>
    public static MeterMetrics GetShared(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return Shared.GetOrAdd(name, static key => new Lazy<MeterMetrics>(() => new MeterMetrics(SharedOpenUrzednikMeter.Get(key)))).Value;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <see langword="true"/> while no instrument has this name yet (the first <see cref="Record"/> creates it), then whether a
    /// listener is enabled for that instrument.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="measurementName"/> is <see langword="null"/>.</exception>
    public bool IsEnabled(string measurementName)
    {
        ArgumentNullException.ThrowIfNull(measurementName);
        return _meter.IsEnabled(measurementName);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="measurement"/> is <see langword="null"/>.</exception>
    public void Record(IOpenUrzednikMeasurement measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        _meter.Record(measurement);
    }
}

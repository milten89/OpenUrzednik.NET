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
/// <see cref="GetShared"/> returns the metrics on the one meter per name for the process, shared with the clients' own
/// default on .NET 8 and later; <see cref="Create"/> returns metrics on a DI container's <see cref="IMeterFactory"/> meter.
/// Histogram bucket advice (<c>InstrumentAdvice</c>) is applied on .NET 10 and
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

    /// <summary>
    /// Returns new metrics on the meter <paramref name="name"/> of <paramref name="meterFactory"/>, e.g. a DI container's
    /// <see cref="IMeterFactory"/>: the meter is disposed with the factory, and tests can tell it apart by its scope (the
    /// factory), e.g. with <c>MetricCollector</c>. The DI packages use it when the container has a factory (ADR-0025).
    /// </summary>
    /// <remarks>
    /// Each call creates new instruments, so create one instance per factory and name and reuse it. The meter has no version
    /// and no tags, like the shared one, so OpenTelemetry merges the two when an app records on both.
    /// </remarks>
    /// <param name="meterFactory">The factory that creates and owns the meter.</param>
    /// <param name="name">The meter name, the provider's source name (e.g. <c>NbpTelemetry.SourceName</c>).</param>
    /// <returns>Metrics on the factory's meter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="meterFactory"/> or <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or white space.</exception>
    public static MeterMetrics Create(IMeterFactory meterFactory, string name)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new MeterMetrics(SharedOpenUrzednikMeter.Over(meterFactory.Create(new MeterOptions(name))));
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

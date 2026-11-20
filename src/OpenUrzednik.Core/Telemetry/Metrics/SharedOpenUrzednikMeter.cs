using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// One <see cref="Meter"/> per name for the whole process, as an <see cref="IOpenUrzednikMetrics"/> (ADR-0025). Each
/// instrument is created from the first measurement recorded with its name (its unit, description and, for a histogram,
/// bucket advice), so measurements added in later versions need no change here. On the .NET targets a provider client
/// without metrics uses it (<c>OpenUrzednik.&lt;Provider&gt;</c>), so <c>AddMeter("OpenUrzednik.*")</c> is enough without
/// DI; <c>MeterMetrics.GetShared</c> in <c>OpenUrzednik.Diagnostics</c> returns the same meter. Compiled into Core on the
/// .NET targets and linked into <c>OpenUrzednik.Diagnostics</c> for netstandard2.0.
/// </summary>
/// <remarks>
/// On .NET, OpenUrzednik.Diagnostics and the providers call <see cref="Get"/> and the members of
/// <see cref="IOpenUrzednikMetrics"/> through <c>InternalsVisibleTo</c>, and an app may combine a newer Core with older
/// packages: within a major version add members, never change or remove these.
/// </remarks>
internal sealed class SharedOpenUrzednikMeter : IOpenUrzednikMetrics
{
    // Lazy, so a race in GetOrAdd can't create (and leave registered) a second meter with the same name.
    private static readonly ConcurrentDictionary<string, Lazy<SharedOpenUrzednikMeter>> Shared = new(StringComparer.Ordinal);

    // The histograms created so far, by instrument name. Only histograms exist today; another instrument kind gets its
    // own dictionary when its measurement interface is added.
    private readonly ConcurrentDictionary<string, Histogram<double>> _histograms = new(StringComparer.Ordinal);

    private SharedOpenUrzednikMeter(Meter meter) => Meter = meter;

    /// <summary>The meter: a shared one lives as long as the process, one passed to <see cref="Over"/> as long as its owner keeps it.</summary>
    internal Meter Meter { get; }

    /// <summary>Returns the metrics for <paramref name="name"/>, creating its <see cref="Meter"/> on first use.</summary>
    internal static SharedOpenUrzednikMeter Get(string name)
        => Shared.GetOrAdd(name, static key => new Lazy<SharedOpenUrzednikMeter>(() => new SharedOpenUrzednikMeter(new Meter(key)))).Value;

    /// <summary>
    /// Returns metrics over a meter the caller owns, e.g. one from an <c>IMeterFactory</c> (ADR-0025): not shared, so create one
    /// per meter and reuse it, or its instruments are created again. Added in 1.1.0 for <c>MeterMetrics.Create</c>.
    /// </summary>
    internal static SharedOpenUrzednikMeter Over(Meter meter) => new(meter);

    /// <inheritdoc/>
    /// <remarks>
    /// <see langword="true"/> while no instrument has this name yet (the first <see cref="Record"/> creates it), then whether
    /// a listener is enabled for that instrument.
    /// </remarks>
    public bool IsEnabled(string measurementName)
        => !_histograms.TryGetValue(measurementName, out var histogram) || histogram.Enabled;

    /// <inheritdoc/>
    public void Record(IOpenUrzednikMeasurement measurement)
    {
        if (measurement is not IOpenUrzednikHistogramMeasurement histogramMeasurement)
        {
            return;
        }

        if (!_histograms.TryGetValue(measurement.Name, out var histogram))
        {
            histogram = GetOrCreateHistogram(histogramMeasurement);
        }

        if (!histogram.Enabled)
        {
            return;
        }

        var source = measurement.Tags;
        var tags = default(TagList);
        for (var i = 0; i < source.Count; i++)
        {
            tags.Add(source[i]);
        }

        histogram.Record(histogramMeasurement.Value, in tags);
    }

    // Once per instrument. A separate method, so the closure is allocated only here and not on every Record (a captured
    // variable of Record would make the compiler create it at the start of Record); the GetOrAdd overload with a state
    // argument doesn't exist on netstandard2.0.
    private Histogram<double> GetOrCreateHistogram(IOpenUrzednikHistogramMeasurement measurement)
        => _histograms.GetOrAdd(measurement.Name, _ => CreateHistogram(Meter, measurement));

    private static Histogram<double> CreateHistogram(Meter meter, IOpenUrzednikHistogramMeasurement measurement)
#if NET9_0_OR_GREATER || NETSTANDARD2_0
        // InstrumentAdvice exists from System.Diagnostics.DiagnosticSource 9.0: built into net10.0, and in the package
        // OpenUrzednik.Diagnostics references on netstandard2.0. net8.0's built-in version has none.
        => meter.CreateHistogram<double>(measurement.Name, measurement.Unit, measurement.Description, tags: null,
            advice: measurement.BucketBoundaries is { } boundaries ? new InstrumentAdvice<double> { HistogramBucketBoundaries = boundaries } : null);
#else
        => meter.CreateHistogram<double>(measurement.Name, measurement.Unit, measurement.Description);
#endif
}

namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// Records the clients' metrics. Every measurement comes through <see cref="Record"/>, described by the measurement
/// interfaces (<see cref="IOpenUrzednikMeasurement"/> and one interface per instrument kind, such as
/// <see cref="IOpenUrzednikHistogramMeasurement"/>), so an implementation can record measurements added in later versions
/// without changes. The <c>OpenUrzednik.Diagnostics</c> package records them on a <c>System.Diagnostics.Metrics.Meter</c>
/// (<c>MeterMetrics</c>).
/// </summary>
/// <remarks>
/// <para>
/// A later minor version may add a new instrument kind as a new measurement interface; an implementation ignores kinds it
/// doesn't know, and the release notes say when one is added (ADR-0025).
/// </para>
/// <para>
/// Many clients call one instance at the same time, so implementations must be thread-safe. The library never passes
/// <see langword="null"/>.
/// </para>
/// </remarks>
public interface IOpenUrzednikMetrics
{
    /// <summary>
    /// Whether a measurement with this name may be recorded. The clients skip measuring when it certainly won't be.
    /// </summary>
    /// <param name="measurementName">The measurement's <see cref="IOpenUrzednikMeasurement.Name"/>.</param>
    /// <returns>
    /// <see langword="false"/> when <see cref="Record"/> certainly wouldn't record a measurement with this name;
    /// <see langword="true"/> when it would, or when the implementation can't tell yet (for example before the measurement's
    /// instrument exists).
    /// </returns>
    bool IsEnabled(string measurementName);

    /// <summary>
    /// Records one measurement. A measurement of a kind the implementation doesn't know is ignored.
    /// </summary>
    /// <param name="measurement">
    /// The measurement; the library creates it. It is valid only during the call: read what you need, don't keep it.
    /// </param>
    void Record(IOpenUrzednikMeasurement measurement);
}

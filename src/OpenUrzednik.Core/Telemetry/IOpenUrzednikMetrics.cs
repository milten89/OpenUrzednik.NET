namespace OpenUrzednik.Core.Telemetry;

/// <summary>
/// Records the clients' metrics. Every measurement comes through <see cref="Record"/>, described by the measurement
/// interfaces (<see cref="IOpenUrzednikMeasurement"/> and one interface per instrument kind, such as
/// <see cref="IOpenUrzednikHistogramMeasurement"/>), so an implementation can record measurements added in later versions
/// without changes. The <c>OpenUrzednik.Diagnostics</c> package records them on a <c>System.Diagnostics.Metrics.Meter</c>
/// (<c>MeterMetrics</c>).
/// </summary>
/// <remarks>
/// A later minor version may add a new instrument kind as a new measurement interface; an implementation ignores kinds it
/// doesn't know, and the release notes say when one is added (ADR-0025).
/// </remarks>
public interface IOpenUrzednikMetrics
{
    /// <summary>
    /// Whether a measurement with this name would be recorded. The clients skip measuring when it wouldn't.
    /// </summary>
    /// <param name="measurementName">The measurement's <see cref="IOpenUrzednikMeasurement.Name"/>.</param>
    /// <returns><see langword="true"/> if <see cref="Record"/> would record a measurement with this name.</returns>
    bool IsEnabled(string measurementName);

    /// <summary>
    /// Records one measurement. A measurement of a kind the implementation doesn't know is ignored.
    /// </summary>
    /// <param name="measurement">The measurement; the library creates it.</param>
    void Record(IOpenUrzednikMeasurement measurement);
}

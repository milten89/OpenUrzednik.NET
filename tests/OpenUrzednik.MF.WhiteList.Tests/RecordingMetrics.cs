using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.MF.WhiteList.Tests;

/// <summary>
/// Metrics that keep every measurement a client records, copied when it is recorded (a measurement is valid only during
/// <see cref="Record"/>), on every target.
/// </summary>
internal sealed class RecordingMetrics : IOpenUrzednikMetrics
{
    private readonly List<(string Name, Dictionary<string, object?> Tags)> _recorded = [];

    public IReadOnlyList<(string Name, Dictionary<string, object?> Tags)> Recorded => _recorded;

    public bool IsEnabled(string measurementName) => true;

    public void Record(IOpenUrzednikMeasurement measurement)
        => _recorded.Add((measurement.Name, measurement.Tags.ToDictionary(tag => tag.Key, tag => tag.Value)));
}

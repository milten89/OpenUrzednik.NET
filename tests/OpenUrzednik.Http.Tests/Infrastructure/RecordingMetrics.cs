using System.Diagnostics;

using OpenUrzednik.Core.Telemetry;

namespace OpenUrzednik.Http.Tests.Infrastructure;

/// <summary>
/// An <see cref="IOpenUrzednikMetrics"/> that copies each measurement when it is recorded (a measurement is valid only during
/// <see cref="Record"/>), with <see cref="Activity.Current"/> at that moment.
/// </summary>
internal sealed class RecordingMetrics(bool enabled = true) : IOpenUrzednikMetrics
{
    private readonly List<RecordedOperation> _recorded = [];
    private readonly List<string> _enabledChecks = [];

    public IReadOnlyList<RecordedOperation> Recorded => _recorded;

    public IReadOnlyList<string> EnabledChecks => _enabledChecks;

    /// <summary>Runs when a measurement is recorded, e.g. to check that the span is still open.</summary>
    public Action? OnRecord { get; set; }

    public bool IsEnabled(string measurementName)
    {
        _enabledChecks.Add(measurementName);
        return enabled;
    }

    public void Record(IOpenUrzednikMeasurement measurement)
    {
        OnRecord?.Invoke();
        var histogram = (IOpenUrzednikHistogramMeasurement)measurement;
        _recorded.Add(new RecordedOperation(measurement.Name, histogram.Value, measurement.Tags.ToDictionary(tag => tag.Key, tag => tag.Value), Activity.Current));
    }
}

internal sealed record RecordedOperation(string Name, double Value, Dictionary<string, object?> Tags, Activity? CurrentActivity);

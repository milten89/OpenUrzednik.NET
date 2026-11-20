using System.Diagnostics.Metrics;

namespace OpenUrzednik.Diagnostics.Tests;

/// <summary>
/// Records the double measurements of one meter. Tests give each meter a unique name, so parallel tests don't see each
/// other's measurements.
/// </summary>
internal sealed class MeterRecorder : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly List<RecordedMeasurement> _measurements = [];
    private readonly List<Instrument> _instruments = [];

    public MeterRecorder(string meterName)
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == meterName)
            {
                lock (_instruments)
                    _instruments.Add(instrument);
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
        {
            lock (_measurements)
                _measurements.Add(new RecordedMeasurement(instrument, value, tags.ToArray()));
        });
        _listener.Start();
    }

    public IReadOnlyList<RecordedMeasurement> Measurements
    {
        get
        {
            lock (_measurements)
                return [.. _measurements];
        }
    }

    public IReadOnlyList<Instrument> Instruments
    {
        get
        {
            lock (_instruments)
                return [.. _instruments];
        }
    }

    public void Dispose() => _listener.Dispose();
}

internal sealed record RecordedMeasurement(Instrument Instrument, double Value, KeyValuePair<string, object?>[] Tags);

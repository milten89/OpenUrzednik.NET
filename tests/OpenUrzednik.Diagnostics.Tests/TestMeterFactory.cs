using System.Diagnostics.Metrics;

namespace OpenUrzednik.Diagnostics.Tests;

/// <summary>
/// An <see cref="IMeterFactory"/> like a DI container's: its meters have it as their scope and are disposed with it.
/// </summary>
internal sealed class TestMeterFactory : IMeterFactory
{
    private readonly List<Meter> _meters = [];

    public Meter Create(MeterOptions options)
    {
        options.Scope = this;
        var meter = new Meter(options);
        lock (_meters)
            _meters.Add(meter);
        return meter;
    }

    public void Dispose()
    {
        lock (_meters)
        {
            foreach (var meter in _meters)
                meter.Dispose();
            _meters.Clear();
        }
    }
}

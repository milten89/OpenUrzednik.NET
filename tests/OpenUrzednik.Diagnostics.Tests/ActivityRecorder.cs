using System.Diagnostics;

namespace OpenUrzednik.Diagnostics.Tests;

/// <summary>
/// An <see cref="ActivitySource"/> with a unique name and a listener that keeps the stopped activities.
/// The unique name keeps tests running in parallel apart.
/// </summary>
internal sealed class ActivityRecorder : IDisposable
{
    private readonly ActivityListener? _listener;

    public ActivityRecorder(ActivitySamplingResult? sampling = ActivitySamplingResult.AllDataAndRecorded)
    {
        Source = new ActivitySource($"OpenUrzednik.Tests.{Guid.NewGuid():N}");
        if (sampling is not { } result)
            return;

        _listener = new ActivityListener
        {
            ShouldListenTo = source => ReferenceEquals(source, Source),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => result,
            ActivityStopped = Stopped.Add,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public ActivitySource Source { get; }

    public List<Activity> Stopped { get; } = [];

    public void Dispose()
    {
        _listener?.Dispose();
        Source.Dispose();
    }
}

using System.Diagnostics;

using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

// Shared helpers; tests are in one file per method.
public sealed partial class ActivitySpanTest : IDisposable
{
    private readonly ActivityRecorder _recorder = new();

    public void Dispose() => _recorder.Dispose();

    private ActivitySpan StartSpan() => new ActivityTraceSource(_recorder.Source).StartSpan("test.span").ShouldBeOfType<ActivitySpan>();

    // Stops the span and returns the recorded activity.
    private Activity Stop(ActivitySpan span)
    {
        span.Dispose();
        return _recorder.Stopped.ShouldHaveSingleItem();
    }
}

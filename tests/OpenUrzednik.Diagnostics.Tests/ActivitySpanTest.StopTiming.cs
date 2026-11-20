using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

public sealed partial class ActivitySpanTest
{
    [Fact]
    public void StopTiming_ThenDispose_KeepsTheEndTimeAndDuration()
    {
        // Arrange
        var span = StartSpan();
        Thread.Sleep(10);

        // Act
        var duration = ((IOpenUrzednikSpanTiming)span).StopTiming();
        Thread.Sleep(30);
        var activity = Stop(span);

        // Assert: Stop() kept the end time StopTiming set, so the later sleep isn't in the trace
        duration.ShouldNotBeNull();
        duration.Value.ShouldBeGreaterThan(TimeSpan.Zero);
        activity.Duration.ShouldBe(duration.Value);
    }
}

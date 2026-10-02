using System.Diagnostics;

using OpenUrzednik.Core.Telemetry;

using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

public sealed partial class ActivityTraceSourceTest
{
    [Fact]
    public void StartSpan_NoListener_ReturnsNoOpSpan()
    {
        // Arrange
        using var recorder = new ActivityRecorder(sampling: null);

        // Act
        using var span = new ActivityTraceSource(recorder.Source).StartSpan("nbp.gold.latest");

        // Assert
        span.ShouldBeSameAs(NullOpenUrzednikTraceSource.Instance.StartSpan("nbp.gold.latest"));
        span.IsRecording.ShouldBeFalse();
    }

    [Fact]
    public void StartSpan_NotSampled_ReturnsNoOpSpan()
    {
        // Arrange
        using var recorder = new ActivityRecorder(ActivitySamplingResult.None);

        // Act
        using var span = new ActivityTraceSource(recorder.Source).StartSpan("nbp.gold.latest");

        // Assert
        span.IsRecording.ShouldBeFalse();
        recorder.Stopped.ShouldBeEmpty();
    }

    [Fact]
    public void StartSpan_Listener_StartsInternalActivityStoppedOnDispose()
    {
        // Arrange
        using var recorder = new ActivityRecorder();

        // Act
        var span = new ActivityTraceSource(recorder.Source).StartSpan("nbp.gold.latest");
        span.Dispose();

        // Assert
        span.IsRecording.ShouldBeTrue();
        var activity = recorder.Stopped.ShouldHaveSingleItem();
        activity.OperationName.ShouldBe("nbp.gold.latest");
        activity.Kind.ShouldBe(ActivityKind.Internal);
    }

    [Fact]
    public void StartSpan_PropagationOnly_StartsActivityThatIsNotRecording()
    {
        // Arrange
        using var recorder = new ActivityRecorder(ActivitySamplingResult.PropagationData);

        // Act
        using var span = new ActivityTraceSource(recorder.Source).StartSpan("nbp.gold.latest");

        // Assert
        span.ShouldBeOfType<ActivitySpan>();
        span.IsRecording.ShouldBeFalse();
    }

    [Fact]
    public void StartSpan_InsideAnotherSpan_StartsChildActivity()
    {
        // Arrange
        using var recorder = new ActivityRecorder();
        var sut = new ActivityTraceSource(recorder.Source);

        // Act
        using (sut.StartSpan("nbp.gold.latest"))
        using (sut.StartSpan("nbp.http.get"))
        {
        }

        // Assert
        recorder.Stopped.Count.ShouldBe(2);
        var child = recorder.Stopped[0];
        var parent = recorder.Stopped[1];
        child.OperationName.ShouldBe("nbp.http.get");
        child.ParentSpanId.ShouldBe(parent.SpanId);
    }
}

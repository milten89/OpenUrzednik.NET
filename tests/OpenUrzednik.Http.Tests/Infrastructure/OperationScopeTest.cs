using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

public class OperationScopeTest
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly RecordingMetrics _metrics = new();

    private OperationScope Measured(IOpenUrzednikSpan span)
        => new(span, new OperationMeasurement("test.operation", "test", "api.example.com", 443, _metrics, _timeProvider));

    [Fact]
    public void Complete_Success_RecordsTheElapsedTimeWithoutErrorType()
    {
        // Arrange
        var scope = Measured(Substitute.For<IOpenUrzednikSpan>());
        _timeProvider.Advance(TimeSpan.FromSeconds(2));

        // Act
        scope.Complete(OpenUrzednikResult.Success(1));

        // Assert
        var recorded = _metrics.Recorded.ShouldHaveSingleItem();
        recorded.Name.ShouldBe("openurzednik.client.operation.duration");
        recorded.Value.ShouldBe(2);
        recorded.Tags.ShouldNotContainKey("error.type");
        recorded.Tags["openurzednik.operation"].ShouldBe("test.operation");
    }

    [Fact]
    public void Complete_Failure_RecordsTheFirstErrorCode()
    {
        // Arrange
        var scope = Measured(Substitute.For<IOpenUrzednikSpan>());

        // Act
        scope.Complete(OpenUrzednikResult.Failure<int>([new NotFoundError("missing"), new UnknownError("other")]));

        // Assert
        _metrics.Recorded.ShouldHaveSingleItem().Tags["error.type"].ShouldBe(NotFoundError.ErrorCode);
    }

    [Fact]
    public void Complete_SpanThatReportsItsDuration_RecordsTheSpansDuration()
    {
        // Arrange
        var span = Substitute.For<IOpenUrzednikSpan, IOpenUrzednikSpanTiming>();
        ((IOpenUrzednikSpanTiming)span).StopTiming().Returns(TimeSpan.FromMilliseconds(1500));
        var scope = Measured(span);
        _timeProvider.Advance(TimeSpan.FromSeconds(9));

        // Act
        scope.Complete(OpenUrzednikResult.Success(1));

        // Assert
        _metrics.Recorded.ShouldHaveSingleItem().Value.ShouldBe(1.5);
    }

    [Fact]
    public void Complete_SpanWithoutADuration_RecordsTheElapsedTime()
    {
        // Arrange
        var span = Substitute.For<IOpenUrzednikSpan, IOpenUrzednikSpanTiming>();
        ((IOpenUrzednikSpanTiming)span).StopTiming().Returns((TimeSpan?)null);
        var scope = Measured(span);
        _timeProvider.Advance(TimeSpan.FromSeconds(3));

        // Act
        scope.Complete(OpenUrzednikResult.Success(1));

        // Assert
        _metrics.Recorded.ShouldHaveSingleItem().Value.ShouldBe(3);
    }

    [Fact]
    public void Complete_AnyResult_ReturnsItUnchanged()
    {
        // Arrange
        var scope = Measured(Substitute.For<IOpenUrzednikSpan>());
        var result = OpenUrzednikResult.Success("value");

        // Act
        var returned = scope.Complete(result);

        // Assert
        returned.ShouldBe(result);
    }

    [Fact]
    public void Complete_BeforeTheSpanEnds_RecordsWhileTheSpanIsOpen()
    {
        // Arrange
        var span = Substitute.For<IOpenUrzednikSpan>();
        var disposedAtRecord = true;
        _metrics.OnRecord = () => disposedAtRecord = span.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IDisposable.Dispose));
        using (var scope = Measured(span))
        {
            // Act
            scope.Complete(OpenUrzednikResult.Success(1));
        }

        // Assert
        disposedAtRecord.ShouldBeFalse();
        span.Received(1).Dispose();
    }

    [Fact]
    public void Complete_Twice_RecordsOnce()
    {
        // Arrange
        var scope = Measured(Substitute.For<IOpenUrzednikSpan>());

        // Act
        scope.Complete(OpenUrzednikResult.Success(1));
        scope.Complete(OpenUrzednikResult.Failure<int>(new UnknownError("late")));

        // Assert
        _metrics.Recorded.ShouldHaveSingleItem().Tags.ShouldNotContainKey("error.type");
    }

    [Fact]
    public void Dispose_WithoutComplete_RecordsOtherAndEndsTheSpan()
    {
        // Arrange
        var span = Substitute.For<IOpenUrzednikSpan>();
        var scope = Measured(span);
        _timeProvider.Advance(TimeSpan.FromSeconds(1));

        // Act
        scope.Dispose();

        // Assert
        var recorded = _metrics.Recorded.ShouldHaveSingleItem();
        recorded.Tags["error.type"].ShouldBe("_OTHER");
        recorded.Tags["error.type"].ShouldBe(OperationScope.OtherErrorType);
        recorded.Value.ShouldBe(1);
        span.Received(1).Dispose();
    }

    [Fact]
    public void Dispose_AfterComplete_DoesNotRecordAgain()
    {
        // Arrange
        var scope = Measured(Substitute.For<IOpenUrzednikSpan>());
        scope.Complete(OpenUrzednikResult.Success(1));

        // Act
        scope.Dispose();

        // Assert
        _metrics.Recorded.ShouldHaveSingleItem().Tags.ShouldNotContainKey("error.type");
    }

    [Fact]
    public void SpanOnly_CompleteAndDispose_RecordNothingAndEndTheSpan()
    {
        // Arrange
        var span = Substitute.For<IOpenUrzednikSpan, IOpenUrzednikSpanTiming>();
        var scope = new OperationScope(span);

        // Act
        var result = scope.Complete(OpenUrzednikResult.Success(1));
        scope.Dispose();

        // Assert
        result.Value.ShouldBe(1);
        scope.Span.ShouldBeSameAs(span);
        ((IOpenUrzednikSpanTiming)span).DidNotReceive().StopTiming();
        span.Received(1).Dispose();
    }
}

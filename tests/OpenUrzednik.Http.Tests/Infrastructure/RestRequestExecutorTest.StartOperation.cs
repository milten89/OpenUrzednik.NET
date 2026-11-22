#if NET
using System.Diagnostics;

#endif
using NSubstitute;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;

using Shouldly;

namespace OpenUrzednik.Http.Tests.Infrastructure;

public partial class RestRequestExecutorTest
{
    [Fact]
    public void StartOperation_MetricEnabled_RecordsWithTheProviderAndServer()
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var tracer = Substitute.For<IOpenUrzednikTraceSource>();
        var span = Substitute.For<IOpenUrzednikSpan>();
        tracer.StartSpan("test.operation").Returns(span);
        var executor = new RestRequestExecutor(new HttpClient(), new Uri("https://api.example.com:8443/base/"), Profile,
            telemetry: new ClientTelemetry(traceSource: tracer, metrics: metrics), timeProvider: _timeProvider);

        // Act
        using (var operation = executor.StartOperation("test.operation"))
        {
            operation.Span.ShouldBeSameAs(span);
            operation.Complete(OpenUrzednikResult.Success(1));
        }

        // Assert
        metrics.EnabledChecks.ShouldBe(["openurzednik.client.operation.duration"]);
        var recorded = metrics.Recorded.ShouldHaveSingleItem();
        recorded.Tags.ShouldBe(new Dictionary<string, object?>
        {
            ["openurzednik.operation"] = "test.operation",
            ["openurzednik.provider"] = "test",
            ["server.address"] = "api.example.com",
            ["server.port"] = 8443,
        });
    }

    [Fact]
    public void StartOperation_MetricDisabled_RecordsNothing()
    {
        // Arrange
        var metrics = new RecordingMetrics(enabled: false);
        var executor = new RestRequestExecutor(new HttpClient(), new Uri("https://api.example.com/"), Profile,
            telemetry: new ClientTelemetry(metrics: metrics), timeProvider: _timeProvider);

        // Act
        using (var operation = executor.StartOperation("test.operation"))
        {
            operation.Complete(OpenUrzednikResult.Success(1));
        }

        // Assert
        metrics.EnabledChecks.ShouldHaveSingleItem();
        metrics.Recorded.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StartOperation_MetricDisabledAndOperationThrows_MarksOnlyARecordingSpanFailed(bool recording)
    {
        // Arrange
        var span = Substitute.For<IOpenUrzednikSpan>();
        span.IsRecording.Returns(recording);
        var tracer = Substitute.For<IOpenUrzednikTraceSource>();
        tracer.StartSpan("test.operation").Returns(span);
        var executor = new RestRequestExecutor(new HttpClient(), new Uri("https://api.example.com/"), Profile,
            telemetry: new ClientTelemetry(traceSource: tracer, metrics: new RecordingMetrics(enabled: false)), timeProvider: _timeProvider);

        // Act: disposed without Complete, as when the operation throws
        executor.StartOperation("test.operation").Dispose();

        // Assert
        span.Received(recording ? 1 : 0).SetTag("error.type", "_OTHER");
        span.Received(recording ? 1 : 0).SetStatus(OpenUrzednikSpanStatus.Error, "Unexpected exception");
        span.Received(1).Dispose();
    }

#if NET
    [Fact]
    public void StartOperation_TracedOperation_RecordsTheActivitysDurationWhileItIsCurrent()
    {
        // Arrange: the .NET default trace source of a provider name, with a listener that samples it
        var sourceName = $"OpenUrzednik.Tests.{Guid.NewGuid():N}";
        Activity? stopped = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => stopped = activity,
        };
        ActivitySource.AddActivityListener(listener);
        var metrics = new RecordingMetrics();
        var executor = new RestRequestExecutor(new HttpClient(), new Uri("https://api.example.com/"), Profile,
            telemetry: new ClientTelemetry(sourceName: sourceName, metrics: metrics), timeProvider: _timeProvider);

        // Act
        using (var operation = executor.StartOperation("test.operation"))
        {
            Thread.Sleep(20);
            operation.Complete(OpenUrzednikResult.Success(1));
            Thread.Sleep(20);
        }

        // Assert: the same duration as the trace (so the second sleep isn't in either), recorded while it was current
        stopped.ShouldNotBeNull();
        var recorded = metrics.Recorded.ShouldHaveSingleItem();
        recorded.Value.ShouldBe(stopped.Duration.TotalSeconds);
        recorded.CurrentActivity.ShouldBeSameAs(stopped);
    }

#endif
}

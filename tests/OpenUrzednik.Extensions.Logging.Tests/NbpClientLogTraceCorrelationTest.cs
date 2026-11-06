using System.Diagnostics;
using System.Net;
using System.Text;

using Microsoft.Extensions.Logging;

using OpenUrzednik.Diagnostics;
using OpenUrzednik.Nbp;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Extensions.Logging.Tests;

// Telemetry contract (ADR-0003): a client writes an operation's log entries while that operation's span, or its HTTP child
// span, is Activity.Current, so log providers can link them to the trace. End-to-end through NbpGoldPriceClient with the real
// ActivityTraceSource and ILogger adapter, so not split into one file per method.
public sealed class NbpClientLogTraceCorrelationTest : IDisposable
{
    private readonly ActivitySource _source = new($"OpenUrzednik.Tests.{Guid.NewGuid():N}");
    private readonly ActivityListener _listener;
    private readonly List<Activity> _stopped = [];
    private readonly RecordingLogger _logger = new();

#if NETFRAMEWORK
    // .NET Framework defaults to hierarchical activity ids, which have no trace id. Correlation needs W3C ids, as the
    // Diagnostics README tells .NET Framework apps (OpenTelemetry sets this itself). Set once for the whole test process, on
    // purpose and never restored: it only changes the id format, which no other test depends on.
    static NbpClientLogTraceCorrelationTest()
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;
    }
#endif

    public NbpClientLogTraceCorrelationTest()
    {
        // A unique source keeps tests running in parallel apart.
        _listener = new ActivityListener
        {
            ShouldListenTo = source => ReferenceEquals(source, _source),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => { lock (_stopped) _stopped.Add(activity); },
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        _source.Dispose();
    }

    private NbpGoldPriceClient CreateSut(HttpClient httpClient)
        => new(httpClient, logger: new OpenUrzednikLogger(_logger), traceSource: new ActivityTraceSource(_source));

    private Activity Operation(string name)
    {
        lock (_stopped)
            return _stopped.Single(a => a.OperationName == name);
    }

    [Fact]
    public async Task GetTopCountAsync_ValidationFails_LogsInsideOperationSpan()
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage()));

        // Act
        await CreateSut(httpClient).GetTopCountAsync(0, TestContext.Current.CancellationToken);

        // Assert
        var entry = _logger.Entries.ShouldHaveSingleItem();
        var operation = Operation("nbp.gold.top_count");
        entry.Activity.ShouldNotBeNull().SpanId.ShouldBe(operation.SpanId);
        entry.Activity.TraceId.ShouldBe(operation.TraceId);
    }

    [Fact]
    public async Task GetLatestAsync_EmptyArray_LogsInsideOperationSpan()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]", Encoding.UTF8, "application/json") };
        using var httpClient = new HttpClient(new StubHttpMessageHandler(response));

        // Act
        await CreateSut(httpClient).GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        var entry = _logger.Entries.Where(e => e.EventId.Id == NbpLogEvents.NoData).ShouldHaveSingleItem();
        var operation = Operation("nbp.gold.latest");
        entry.Activity.ShouldNotBeNull().SpanId.ShouldBe(operation.SpanId);
    }

    [Fact]
    public async Task GetLatestAsync_ServerError_LogsInsideHttpChildSpan()
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        // Act
        await CreateSut(httpClient).GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        var operation = Operation("nbp.gold.latest");
        var http = Operation("nbp.http.get");
        http.ParentSpanId.ShouldBe(operation.SpanId);
        var warnings = _logger.Entries.Where(e => e.Level >= LogLevel.Warning).ToList();
        warnings.ShouldNotBeEmpty();
        warnings.ShouldAllBe(e => e.Activity != null && e.Activity.SpanId == http.SpanId && e.Activity.TraceId == operation.TraceId);
    }

    [Fact]
    public async Task GetLatestAsync_ConnectionFails_LogsInsideHttpChildSpan()
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpRequestException("Connection refused")));

        // Act
        await CreateSut(httpClient).GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        var http = Operation("nbp.http.get");
        var entry = _logger.Entries.Where(e => e.EventId.Id == NbpLogEvents.ConnectionFailed).ShouldHaveSingleItem();
        entry.Activity.ShouldNotBeNull().SpanId.ShouldBe(http.SpanId);
        entry.Activity.TraceId.ShouldBe(Operation("nbp.gold.latest").TraceId);
    }

    [Fact]
    public async Task GetLatestAsync_EveryEntry_HasTheOperationsTraceId()
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound)));

        // Act
        await CreateSut(httpClient).GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        var traceId = Operation("nbp.gold.latest").TraceId;
        _logger.Entries.ShouldNotBeEmpty();
        _logger.Entries.ShouldAllBe(e => e.Activity != null && e.Activity.TraceId == traceId);
    }
}

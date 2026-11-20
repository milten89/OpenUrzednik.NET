#if NET
using System.Diagnostics.Metrics;
#endif
using System.Net;

#if NET
using Microsoft.Extensions.Time.Testing;

#endif
using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.MF.WhiteList.Common;
#if NET
using OpenUrzednik.TestCommon;
#endif

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Common;

// ADR-0025: a White List client method starts its operation through the pipeline, and the operation records its duration with the
// provider's tags. The clients come later (#259–#262); these tests run one operation the way a client method will.
public sealed partial class WhiteListRequestPipelineTest
{
    private const string OperationName = "mf_white_list.search.nip";

    [Fact]
    public async Task StartOperation_QuotaRejection_RecordsTheRateLimitErrorType()
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Json((HttpStatusCode)429, """{"message":"Limit zapytań został wyczerpany.","code":"WL-191"}"""), out _,
            new ClientTelemetry(metrics: metrics));

        // Act
        await RunOperationAsync(sut);

        // Assert
        var tags = metrics.Recorded.ShouldHaveSingleItem().Tags;
        tags["error.type"].ShouldBe(RateLimitExceededError.ErrorCode);
        tags["openurzednik.operation"].ShouldBe(OperationName);
        tags["openurzednik.provider"].ShouldBe(WhiteListConnection.ProviderName);
        tags["server.address"].ShouldBe("wykazpodatnikowvat.mf.gov.pl");
        tags["server.port"].ShouldBe(443);
    }

    [Fact]
    public async Task StartOperation_Success_RecordsWithoutErrorType()
    {
        // Arrange
        var metrics = new RecordingMetrics();
        var sut = CreateSut(Json(HttpStatusCode.OK, """{"message":null,"code":"OK"}"""), out _, new ClientTelemetry(metrics: metrics));

        // Act
        var result = await RunOperationAsync(sut);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var recorded = metrics.Recorded.ShouldHaveSingleItem();
        recorded.Name.ShouldBe("openurzednik.client.operation.duration");
        recorded.Tags.ShouldNotContainKey("error.type");
    }

#if NET
    [Fact]
    public async Task StartOperation_DefaultMetrics_RecordsOnTheWhiteListMeter()
    {
        // Arrange: on .NET a client without metrics records on the shared OpenUrzednik.MF.WhiteList meter; other tests record on it at
        // the same time, so this one keeps only the measurements of its own host
        var host = $"{Guid.NewGuid():N}.example.com";
        var operations = new List<string>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == WhiteListTelemetry.SourceName)
                    l.EnableMeasurementEvents(instrument);
            },
        };
        listener.SetMeasurementEventCallback<double>((instrument, _, tags, _) =>
        {
            var ownHost = false;
            foreach (var tag in tags)
                ownHost |= tag.Key == "server.address" && Equals(tag.Value, host);
            if (ownHost)
            {
                lock (operations)
                    operations.Add(instrument.Name);
            }
        });
        listener.Start();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(Json(HttpStatusCode.OK, """{"message":null,"code":"OK"}""")));
        var telemetry = new ClientTelemetry(sourceName: WhiteListTelemetry.SourceName);
        var sut = new WhiteListRequestPipeline(
            WhiteListConnection.CreateSearch(httpClient, new WhiteListOptions { ApiUrl = $"https://{host}/" }, telemetry, new FakeTimeProvider()),
            telemetry);

        // Act
        await RunOperationAsync(sut);

        // Assert
        operations.ShouldBe(["openurzednik.client.operation.duration"]);
    }
#endif

    // One operation the way a client method runs it: start, request, complete, dispose.
    private static async Task<OpenUrzednikResult<string?>> RunOperationAsync(WhiteListRequestPipeline sut)
    {
        using var operation = sut.StartOperation(OperationName);
        return operation.Complete(await sut.GetAsync(operation.Span, "SearchNip", OpenUrzednikResult.Success(), () => RelativePath, DisplayPath,
            WhiteListJsonContext.Default.ErrorDto, dto => dto.Code, TestContext.Current.CancellationToken));
    }

    // Copies each measurement when it is recorded (a measurement is valid only during Record).
    private sealed class RecordingMetrics : IOpenUrzednikMetrics
    {
        private readonly List<(string Name, Dictionary<string, object?> Tags)> _recorded = [];

        public IReadOnlyList<(string Name, Dictionary<string, object?> Tags)> Recorded => _recorded;

        public bool IsEnabled(string measurementName) => true;

        public void Record(IOpenUrzednikMeasurement measurement)
            => _recorded.Add((measurement.Name, measurement.Tags.ToDictionary(tag => tag.Key, tag => tag.Value)));
    }
}

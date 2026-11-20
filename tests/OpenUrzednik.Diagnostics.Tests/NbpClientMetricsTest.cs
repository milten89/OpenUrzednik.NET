using System.Net;
#if NET
using System.Text;
#endif

#if NET
using OpenUrzednik.Core.Errors;
#endif
using OpenUrzednik.Nbp;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

// ADR-0025 confirmation: an operation of a real client reaches a MeterListener on the provider's meter. The NBP clients
// record on the shared OpenUrzednik.Nbp meter by default on .NET; each test uses its own API host and keeps only its own
// measurements, because other tests record on that meter at the same time. End-to-end, so not split per method.
public sealed class NbpClientMetricsTest : IDisposable
{
    private readonly MeterRecorder _recorder = new(NbpTelemetry.SourceName);
    private readonly string _host = $"{Guid.NewGuid():N}.example.com";

    public void Dispose() => _recorder.Dispose();

    private NbpGoldPriceClient CreateSut(HttpResponseMessage response)
        => new(new HttpClient(new StubHttpMessageHandler(response)), new NbpOptions { ApiUrl = $"https://{_host}/api/" });

    private RecordedMeasurement[] Own()
        => _recorder.Measurements.Where(m => m.Tags.Any(tag => tag.Key == "server.address" && Equals(tag.Value, _host))).ToArray();

#if NET
    [Fact]
    public async Task GetLatestAsync_Success_RecordsTheOperationDuration()
    {
        // Arrange
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"data":"2026-10-02","cena":517.55}]""", Encoding.UTF8, "application/json"),
        });

        // Act
        var result = await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var measurement = Own().ShouldHaveSingleItem();
        measurement.Instrument.Name.ShouldBe("openurzednik.client.operation.duration");
        measurement.Instrument.Unit.ShouldBe("s");
        measurement.Value.ShouldBeGreaterThanOrEqualTo(0);
        measurement.Tags.ShouldBe(
        [
            new("openurzednik.operation", "nbp.gold.latest"),
            new("openurzednik.provider", "nbp"),
            new("server.address", _host),
            new("server.port", 443),
        ]);
    }

    [Fact]
    public async Task GetLatestAsync_NotFound_RecordsTheErrorType()
    {
        // Arrange
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var result = await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        Own().ShouldHaveSingleItem().Tags.ShouldContain(new KeyValuePair<string, object?>("error.type", NotFoundError.ErrorCode));
    }
#else
    [Fact]
    public async Task GetLatestAsync_NetFrameworkWithoutMetrics_RecordsNothing()
    {
        // Arrange: on netstandard2.0 a client without metrics records nothing (no Meter without DiagnosticSource)
        var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        Own().ShouldBeEmpty();
    }
#endif
}

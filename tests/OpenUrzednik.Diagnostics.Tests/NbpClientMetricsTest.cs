using System.Net;
#if NET
using System.Text;
#endif

using OpenUrzednik.Core.Errors;
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

    // A method that lost its operation.Complete(...) would record error.type _OTHER here instead of the result's error.
    [Fact]
    public async Task CurrencyClient_NotFound_RecordsTheOperationWithTheErrorType()
    {
        // Arrange
        var sut = new NbpCurrencyExchangeRateClient(new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound))),
            new NbpOptions { ApiUrl = $"https://{_host}/api/" });

        // Act
        await sut.GetLatestAsync("USD", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var tags = Own().ShouldHaveSingleItem().Tags;
        tags.ShouldContain(new KeyValuePair<string, object?>("openurzednik.operation", "nbp.currency.latest"));
        tags.ShouldContain(new KeyValuePair<string, object?>("error.type", NotFoundError.ErrorCode));
    }

    [Fact]
    public async Task TableClient_NotFound_RecordsTheOperationWithTheErrorType()
    {
        // Arrange
        var sut = new NbpExchangeRateTableClient(new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound))),
            new NbpOptions { ApiUrl = $"https://{_host}/api/" });

        // Act
        await sut.GetBuySellLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        var tags = Own().ShouldHaveSingleItem().Tags;
        tags.ShouldContain(new KeyValuePair<string, object?>("openurzednik.operation", "nbp.table.buy_sell_latest"));
        tags.ShouldContain(new KeyValuePair<string, object?>("error.type", NotFoundError.ErrorCode));
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

    // Each client takes the metrics through its constructor (ADR-0025): here a MeterMetrics of its own meter, on every target.
    public static TheoryData<string> Clients => ["gold", "currency", "table"];

    [Theory]
    [MemberData(nameof(Clients))]
    public async Task Constructor_MeterMetrics_RecordsOnThatMeter(string client)
    {
        // Arrange
        var meterName = $"OpenUrzednik.Tests.{Guid.NewGuid():N}";
        using var recorder = new MeterRecorder(meterName);
        var metrics = MeterMetrics.GetShared(meterName);
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var options = new NbpOptions { ApiUrl = $"https://{_host}/api/" };
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var operation = client switch
        {
            "gold" => await new NbpGoldPriceClient(httpClient, options, metrics: metrics).GetLatestAsync(cancellationToken).ContinueWith(_ => "nbp.gold.latest", cancellationToken),
            "currency" => await new NbpCurrencyExchangeRateClient(httpClient, options, metrics: metrics).GetLatestAsync("USD", cancellationToken: cancellationToken).ContinueWith(_ => "nbp.currency.latest", cancellationToken),
            _ => await new NbpExchangeRateTableClient(httpClient, options, metrics: metrics).GetBuySellLatestAsync(cancellationToken).ContinueWith(_ => "nbp.table.buy_sell_latest", cancellationToken),
        };

        // Assert
        var measurement = recorder.Measurements.ShouldHaveSingleItem();
        measurement.Instrument.Name.ShouldBe("openurzednik.client.operation.duration");
        measurement.Tags.ShouldContain(new KeyValuePair<string, object?>("openurzednik.operation", operation));
        measurement.Tags.ShouldContain(new KeyValuePair<string, object?>("server.address", _host));
        measurement.Tags.ShouldContain(new KeyValuePair<string, object?>("error.type", NotFoundError.ErrorCode));
    }

    [Fact]
    public async Task ObsoleteConstructor_StillWorks_AndRecordsWithTheDefault()
    {
        // Arrange: the 1.0 constructor, obsolete until 2.0
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound)));
#pragma warning disable CS0618 // the obsolete constructor is what this test checks
        var sut = new NbpGoldPriceClient(httpClient, new NbpOptions { ApiUrl = $"https://{_host}/api/" }, null, null, null);
#pragma warning restore CS0618

        // Act
        var result = await sut.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert: the default metrics, the shared OpenUrzednik.Nbp meter on .NET and none on .NET Framework
        result.IsFailure.ShouldBeTrue();
#if NET
        Own().ShouldHaveSingleItem().Tags.ShouldContain(new KeyValuePair<string, object?>("openurzednik.operation", "nbp.gold.latest"));
#else
        Own().ShouldBeEmpty();
#endif
    }
}

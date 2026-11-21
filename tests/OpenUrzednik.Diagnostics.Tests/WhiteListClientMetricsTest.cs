using System.Net;
using System.Text;

using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.MF.WhiteList;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Diagnostics.Tests;

// ADR-0025 confirmation: an operation of the White List client reaches a MeterListener on the provider's meter. On .NET the client
// records on the shared OpenUrzednik.MF.WhiteList meter by default; each test uses its own API host and keeps only its own measurements,
// because other tests record on that meter at the same time. End-to-end, so not split per method.
public sealed class WhiteListClientMetricsTest : IDisposable
{
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 11, 20, 11, 0, 0, TimeSpan.Zero));
    private static readonly DateTime Date = new(2026, 11, 2);

    private readonly MeterRecorder _recorder = new(WhiteListTelemetry.SourceName);
    private readonly string _host = $"{Guid.NewGuid():N}.example.com";

    public void Dispose() => _recorder.Dispose();

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string body)
        => new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private RecordedMeasurement[] Own()
        => _recorder.Measurements.Where(m => m.Tags.Any(tag => tag.Key == "server.address" && Equals(tag.Value, _host))).ToArray();

#if NET
    [Fact]
    public async Task SearchByRegonAsync_DefaultMetrics_RecordsTheOperationWithTheRateLimitErrorType()
    {
        // Arrange
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            Json((HttpStatusCode)429, """{"message":"Limit żądań dla tego adresu IP został na dziś wyczerpany.","code":"WL-191"}""")));
        var sut = new WhiteListClient(httpClient, new WhiteListOptions { ApiUrl = $"https://{_host}/" }, Clock);

        // Act
        await sut.SearchByRegonAsync("593728162", Date, TestContext.Current.CancellationToken);

        // Assert
        var measurement = Own().ShouldHaveSingleItem();
        measurement.Instrument.Name.ShouldBe("openurzednik.client.operation.duration");
        measurement.Instrument.Unit.ShouldBe("s");
        measurement.Tags.ShouldBe(
        [
            new("openurzednik.operation", "mf_white_list.search.regon"),
            new("openurzednik.provider", "mf_white_list"),
            new("error.type", RateLimitExceededError.ErrorCode),
            new("server.address", _host),
            new("server.port", 443),
        ], ignoreOrder: true);
    }
#else
    [Fact]
    public async Task SearchByRegonAsync_NetFrameworkWithoutMetrics_RecordsNothing()
    {
        // Arrange: on netstandard2.0 a client without metrics records nothing (no Meter without DiagnosticSource)
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var sut = new WhiteListClient(httpClient, new WhiteListOptions { ApiUrl = $"https://{_host}/" }, Clock);

        // Act
        await sut.SearchByRegonAsync("593728162", Date, TestContext.Current.CancellationToken);

        // Assert
        Own().ShouldBeEmpty();
    }
#endif

    [Fact]
    public async Task Constructor_MeterMetrics_RecordsOnThatMeter()
    {
        // Arrange: the metrics through the constructor (ADR-0025), on every target
        var meterName = $"OpenUrzednik.Tests.{Guid.NewGuid():N}";
        using var recorder = new MeterRecorder(meterName);
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            Json(HttpStatusCode.OK, """{"result":{"subjects":[],"requestDateTime":"20-11-2026 22:44:25","requestId":"Meupq-99alll1"}}""")));
        var sut = new WhiteListClient(httpClient, new WhiteListOptions { ApiUrl = $"https://{_host}/" }, Clock,
            new OpenUrzednikTelemetry { Metrics = MeterMetrics.GetShared(meterName) });

        // Act
        await sut.SearchByBankAccountAsync("61109010140000071219812874", Date, TestContext.Current.CancellationToken);

        // Assert
        var measurement = recorder.Measurements.ShouldHaveSingleItem();
        measurement.Tags.ShouldContain(new KeyValuePair<string, object?>("openurzednik.operation", "mf_white_list.search.bank_account"));
        measurement.Tags.ShouldContain(new KeyValuePair<string, object?>("error.type", NotFoundError.ErrorCode));
        measurement.Tags.ShouldAllBe(tag => !$"{tag.Value}".Contains("10901"));
    }

    [Fact]
    public async Task Constructor_MeterMetrics_BatchWithAFailedEntryRecordsNoErrorType()
    {
        // Arrange: an account no subject on the list has; the batch call itself succeeds (ADR-0023)
        var meterName = $"OpenUrzednik.Tests.{Guid.NewGuid():N}";
        using var recorder = new MeterRecorder(meterName);
        using var httpClient = new HttpClient(new StubHttpMessageHandler(Json(HttpStatusCode.OK,
            """{"result":{"entries":[{"identifier":"61109010140000071219812874","subjects":[]}],"requestDateTime":"21-11-2026 08:48:50","requestId":"3h7uz-99b0ck2"}}""")));
        var sut = new WhiteListClient(httpClient, new WhiteListOptions { ApiUrl = $"https://{_host}/" }, Clock,
            new OpenUrzednikTelemetry { Metrics = MeterMetrics.GetShared(meterName) });

        // Act
        var result = await sut.SearchByBankAccountsAsync(["61109010140000071219812874"], Date, TestContext.Current.CancellationToken);

        // Assert
        result.Value.Entries.ShouldHaveSingleItem().IsSuccess.ShouldBeFalse();
        var measurement = recorder.Measurements.ShouldHaveSingleItem();
        measurement.Tags.ShouldContain(new KeyValuePair<string, object?>("openurzednik.operation", "mf_white_list.search.bank_accounts"));
        measurement.Tags.ShouldNotContain(tag => tag.Key == "error.type");
        measurement.Tags.ShouldAllBe(tag => !$"{tag.Value}".Contains("10901"));
    }
}

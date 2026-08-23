using System.Diagnostics;
using System.Net;

using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Options;
using OpenUrzednik.Nbp.UrlBuilder;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class NbpGoldPriceClientWireMockTest : IDisposable
{
    private const string BasePath = "/cenyzlota";

    private readonly WireMockServer _server = WireMockServer.Start(new WireMockServerSettings() { UseSSL = true });
    private readonly NbpUrlBuilderFactory _urlBuilderFactory = new();
    private readonly List<HttpClient> _httpClients = new();

    private NbpGoldPriceClient CreateSut(TimeSpan? timeout = null)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        var httpClient = new HttpClient(handler).ConfigureForNbpApi(new NbpOptions
        {
            ApiUrl = _server.Urls[0],
            Timeout = timeout ?? NbpOptions.DefaultTimeout
        });
        _httpClients.Add(httpClient);
        return new NbpGoldPriceClient(httpClient, _urlBuilderFactory);
    }

    [Fact]
    public async Task GetLatestAsync_Returns200WithData_MapsToGoldPrice()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath(BasePath)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""[{"data":"2026-08-20","cena":522.32}]"""));

        // Act
        var result = await CreateSut().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new GoldPrice(new DateOnly(2026, 8, 20), 522.32m));
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == BasePath &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetTopCountAsync_Returns200WithData_MapsToGoldPrice()
    {
        // Arrange
        const int count = 3;
        var path = $"{BasePath}/last/{count}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          [
                          {"data":"2026-08-18","cena":524.63},
                          {"data":"2026-08-19","cena":526.85},
                          {"data":"2026-08-20","cena":522.32}
                          ]
                          """));

        // Act
        var result = await CreateSut().GetTopCountAsync(count, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(count);
        result.Value[0].ShouldBe(new GoldPrice(new DateOnly(2026, 8, 18), 524.63m));
        result.Value[1].ShouldBe(new GoldPrice(new DateOnly(2026, 8, 19), 526.85m));
        result.Value[2].ShouldBe(new GoldPrice(new DateOnly(2026, 8, 20), 522.32m));
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetTodayAsync_Returns200WithData_MapsToGoldPrice()
    {
        // Arrange
        var path = $"{BasePath}/today";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""[{"data":"2026-08-20","cena":522.32}]"""));

        // Act
        var result = await CreateSut().GetTodayAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new GoldPrice(new DateOnly(2026, 8, 20), 522.32m));
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetAsync_SpecificDate_Returns200WithData_MapsToGoldPrice()
    {
        // Arrange
        var date = new DateOnly(2026, 8, 20);
        var path = $"{BasePath}/{date:O}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""[{"data":"2026-08-20","cena":522.32}]"""));

        // Act
        var result = await CreateSut().GetAsync(date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new GoldPrice(new DateOnly(2026, 8, 20), 522.32m));
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetAsync_DateRange_Returns200WithData_MapsToGoldPrice()
    {
        // Arrange
        var from = new DateOnly(2026, 8, 19);
        var to = new DateOnly(2026, 8, 20);
        var path = $"{BasePath}/{from:O}/{to:O}";
        _server.Given(Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          [
                          {"data":"2026-08-19","cena":526.85},
                          {"data":"2026-08-20","cena":522.32}
                          ]
                          """));

        // Act
        var result = await CreateSut().GetAsync(from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].ShouldBe(new GoldPrice(new DateOnly(2026, 8, 19), 526.85m));
        result.Value[1].ShouldBe(new GoldPrice(new DateOnly(2026, 8, 20), 522.32m));
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == path &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task Timeout_ThrowsOperationCanceledException()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath(BasePath)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody("[]")
                .WithDelay(TimeSpan.FromSeconds(5)));
        var stopwatch = Stopwatch.StartNew();

        // Act
        await Should.ThrowAsync<OperationCanceledException>(async () => await CreateSut(timeout: TimeSpan.FromSeconds(0.1)).GetLatestAsync(TestContext.Current.CancellationToken));

        // Assert
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CancelBeforeTimeout_ThrowsOperationCanceledException()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath(BasePath)
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody("[]")
                .WithDelay(TimeSpan.FromSeconds(5)));
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));
        var stopwatch = Stopwatch.StartNew();

        // Act
        await Should.ThrowAsync<OperationCanceledException>(async () => await CreateSut().GetLatestAsync(cts.Token));

        // Assert
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    public void Dispose()
    {
        foreach (var httpClient in _httpClients)
            httpClient.Dispose();
        _server.Dispose();
    }
}

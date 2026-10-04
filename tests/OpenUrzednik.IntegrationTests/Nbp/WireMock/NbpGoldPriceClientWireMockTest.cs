using System.Diagnostics;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Options;

using Shouldly;

using WireMock.Server;
using WireMock.Settings;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class NbpGoldPriceClientWireMockTest : IDisposable
{
    private const string BasePath = "/cenyzlota";

    private readonly WireMockServer _server = WireMockServer.Start(new WireMockServerSettings() { UseSSL = true });
    private readonly List<HttpClient> _httpClients = new();

    private NbpGoldPriceClient CreateSut(TimeSpan? timeout = null)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        // A plain HttpClient: the base URL and timeout go to the client, the HttpClient isn't configured.
        var httpClient = new HttpClient(handler);
        _httpClients.Add(httpClient);
        return new NbpGoldPriceClient(httpClient, new NbpOptions { ApiUrl = _server.Urls[0], Timeout = timeout });
    }

    [Fact]
    public async Task GetLatestAsync_Returns200WithData_MapsToGoldPrice()
    {
        // Arrange
        _server.GivenFixture(BasePath, "gold-latest.json");

        // Act
        var result = await CreateSut().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new GoldPrice(new DateOnly(2026, 10, 2), 517.55m));
        _server.ShouldHaveReceivedGet(BasePath);
    }

    [Fact]
    public async Task GetTopCountAsync_Returns200WithData_MapsToGoldPrice()
    {
        // Arrange
        const int count = 3;
        var path = $"{BasePath}/last/{count}";
        _server.GivenFixture(path, "gold-last-3.json");

        // Act
        var result = await CreateSut().GetTopCountAsync(count, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([
            new GoldPrice(new DateOnly(2026, 9, 30), 512.85m),
            new GoldPrice(new DateOnly(2026, 10, 1), 518.14m),
            new GoldPrice(new DateOnly(2026, 10, 2), 517.55m),
        ]);
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetTodayAsync_Returns200WithData_MapsToGoldPrice()
    {
        // Arrange
        // The today endpoint returns the same body as the date endpoint on a publication day.
        var path = $"{BasePath}/today";
        _server.GivenFixture(path, "gold-date.json");

        // Act
        var result = await CreateSut().GetTodayAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new GoldPrice(new DateOnly(2026, 9, 30), 512.85m));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetAsync_SpecificDate_Returns200WithData_MapsToGoldPrice()
    {
        // Arrange
        var date = new DateOnly(2026, 9, 30);
        var path = $"{BasePath}/{date.ToIso()}";
        _server.GivenFixture(path, "gold-date.json");

        // Act
        var result = await CreateSut().GetAsync(date, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new GoldPrice(date, 512.85m));
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetAsync_DateRange_Returns200WithData_MapsToGoldPrice()
    {
        // Arrange
        var from = new DateOnly(2026, 9, 28);
        var to = new DateOnly(2026, 9, 30);
        var path = $"{BasePath}/{from.ToIso()}/{to.ToIso()}";
        _server.GivenFixture(path, "gold-range.json");

        // Act
        var result = await CreateSut().GetAsync(from, to, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([
            new GoldPrice(new DateOnly(2026, 9, 28), 530.05m),
            new GoldPrice(new DateOnly(2026, 9, 29), 512.64m),
            new GoldPrice(new DateOnly(2026, 9, 30), 512.85m),
        ]);
        _server.ShouldHaveReceivedGet(path);
    }

    [Fact]
    public async Task GetLatestAsync_Timeout_ReturnsRequestTimeoutError()
    {
        // Arrange
        _server.GivenSlowResponse(BasePath);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await CreateSut(timeout: TimeSpan.FromSeconds(0.1)).GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        stopwatch.Elapsed.ShouldBeLessThan(NbpWireMockServerExtensions.GaveUpWithin);
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeOfType<RequestTimeoutError>().Timeout.ShouldBe(TimeSpan.FromSeconds(0.1));
    }

    [Fact]
    public async Task GetLatestAsync_CancelledBeforeTimeout_ThrowsOperationCanceledException()
    {
        // Arrange
        _server.GivenSlowResponse(BasePath);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));
        var stopwatch = Stopwatch.StartNew();

        // Act
        await Should.ThrowAsync<OperationCanceledException>(async () => await CreateSut().GetLatestAsync(cts.Token));

        // Assert
        stopwatch.Elapsed.ShouldBeLessThan(NbpWireMockServerExtensions.GaveUpWithin);
    }

    public void Dispose()
    {
        foreach (var httpClient in _httpClients)
            httpClient.Dispose();
        _server.Dispose();
    }
}

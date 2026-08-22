using System.Diagnostics;
using System.Net;

using OpenUrzednik.Nbp.Gold;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class DefaultNbpGoldPriceClientWireMockTest
{
    private const string GetLatestPath = "/cenyzlota";

    [Fact]
    public async Task GetLatestAsync_Returns200WithData_MapsToGoldPrice()
    {
        // Arrange
        _server.Given(Request.Create()
                             .WithPath(GetLatestPath)
                             .UsingGet())
               .RespondWith(Response.Create()
                                    .WithStatusCode(HttpStatusCode.OK)
                                    .WithHeader("Content-Type", "application/json")
                                    .WithBody("""[{"data":"2026-08-20","cena":365.12}]"""));

        // Act
        var result = await CreateSut().GetLatestAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new GoldPrice(new DateOnly(2026, 8, 20), 365.12m));
    }

    [Fact]
    public async Task GetLatestAsync_SendsExpectedMethodPathAndAcceptHeader()
    {
        // Arrange
        _server.Given(Request.Create()
                             .WithPath(GetLatestPath)
                             .UsingGet())
               .RespondWith(Response.Create()
                                    .WithStatusCode(HttpStatusCode.OK)
                                    .WithBody("[]"));

        // Act
        await CreateSut().GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        _server.LogEntries.ShouldContain(e =>
            e.RequestMessage!.Method == "GET" &&
            e.RequestMessage.Path == "/cenyzlota" &&
            e.RequestMessage.Headers!["Accept"].Contains("application/json"));
    }

    [Fact]
    public async Task GetLatestAsync_Timeout_ThrowsOperationCanceledException()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath(GetLatestPath)
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
    public async Task GetLatestAsync_CancelBeforeTimeout_ThrowsOperationCanceledException()
    {
        // Arrange
        _server.Given(Request.Create()
                .WithPath(GetLatestPath)
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
}

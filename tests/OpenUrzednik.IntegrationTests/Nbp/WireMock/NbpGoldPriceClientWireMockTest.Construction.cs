using System.Net;

using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Options;

using Shouldly;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

// ADR-0007: the one-line constructor, and one HttpClient shared by clients with different base URLs.
public partial class NbpGoldPriceClientWireMockTest
{
    private HttpClient CreatePlainHttpClient()
    {
        var httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
        _httpClients.Add(httpClient);
        return httpClient;
    }

    private void GivenGoldPrice(string path)
        => _server.Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""[{"data":"2026-08-20","cena":522.32}]"""));

    [Fact]
    public async Task GetLatestAsync_OneLineConstructorWithHttpClientBaseAddress_CallsBaseAddress()
    {
        // Arrange
        GivenGoldPrice("/nbp/cenyzlota");
        var httpClient = CreatePlainHttpClient();
        httpClient.BaseAddress = new Uri($"{_server.Urls[0]}/nbp");

        // Act
        var result = await new NbpGoldPriceClient(httpClient).GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _server.LogEntries.ShouldContain(e => e.RequestMessage!.Path == "/nbp/cenyzlota");
    }

    [Fact]
    public async Task GetLatestAsync_SharedHttpClientWithDifferentApiUrls_EachClientCallsItsOwnBaseUrl()
    {
        // Arrange
        GivenGoldPrice("/first/cenyzlota");
        GivenGoldPrice("/second/cenyzlota");
        var httpClient = CreatePlainHttpClient();
        var first = new NbpGoldPriceClient(httpClient, new NbpOptions { ApiUrl = $"{_server.Urls[0]}/first" });
        var second = new NbpGoldPriceClient(httpClient, new NbpOptions { ApiUrl = $"{_server.Urls[0]}/second" });

        // Act
        var firstResult = await first.GetLatestAsync(TestContext.Current.CancellationToken);
        var secondResult = await second.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        firstResult.IsSuccess.ShouldBeTrue();
        secondResult.IsSuccess.ShouldBeTrue();
        _server.LogEntries.ShouldContain(e => e.RequestMessage!.Path == "/first/cenyzlota");
        _server.LogEntries.ShouldContain(e => e.RequestMessage!.Path == "/second/cenyzlota");
        httpClient.BaseAddress.ShouldBeNull();
    }
}

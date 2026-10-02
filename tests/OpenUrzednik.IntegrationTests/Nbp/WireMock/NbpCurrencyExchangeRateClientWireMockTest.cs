using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Options;

using WireMock.Server;
using WireMock.Settings;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class NbpCurrencyExchangeRateClientWireMockTest : IDisposable
{
    private const string BasePath = "/exchangerates/rates";

    private readonly WireMockServer _server = WireMockServer.Start(new WireMockServerSettings() { UseSSL = true });
    private readonly List<HttpClient> _httpClients = new();

    private NbpCurrencyExchangeRateClient CreateSut(TimeSpan? timeout = null)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        // A plain HttpClient: the base URL and timeout go to the client, the HttpClient isn't configured.
        var httpClient = new HttpClient(handler);
        _httpClients.Add(httpClient);
        return new NbpCurrencyExchangeRateClient(httpClient, new NbpOptions { ApiUrl = _server.Urls[0], Timeout = timeout });
    }

    public void Dispose()
    {
        foreach (var httpClient in _httpClients)
            httpClient.Dispose();
        _server.Dispose();
    }
}

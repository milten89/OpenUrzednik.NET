using OpenUrzednik.Nbp.Options;
using OpenUrzednik.Nbp.Table;

using WireMock.Server;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class NbpExchangeRateTableClientWireMockTest : IDisposable
{
    private const string BasePath = "/exchangerates/tables";

    private readonly WireMockServer _server = LoopbackWireMockServer.StartHttps();
    private readonly List<HttpClient> _httpClients = new();

    private NbpExchangeRateTableClient CreateSut(TimeSpan? timeout = null)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        // A plain HttpClient: the base URL and timeout go to the client, the HttpClient isn't configured.
        var httpClient = new HttpClient(handler);
        _httpClients.Add(httpClient);
        return new NbpExchangeRateTableClient(httpClient, new NbpOptions { ApiUrl = _server.Urls[0], Timeout = timeout });
    }

    public void Dispose()
    {
        foreach (var httpClient in _httpClients)
            httpClient.Dispose();
        _server.Dispose();
    }
}

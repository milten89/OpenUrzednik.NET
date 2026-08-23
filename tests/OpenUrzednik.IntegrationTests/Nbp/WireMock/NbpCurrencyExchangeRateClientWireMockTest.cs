using OpenUrzednik.Nbp.Currency;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Options;
using OpenUrzednik.Nbp.UrlBuilder;

using WireMock.Server;
using WireMock.Settings;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class NbpCurrencyExchangeRateClientWireMockTest : IDisposable
{
    private const string BasePath = "/exchangerates/rates";

    private readonly WireMockServer _server = WireMockServer.Start(new WireMockServerSettings() { UseSSL = true });
    private readonly NbpUrlBuilderFactory _urlBuilderFactory = new();
    private readonly List<HttpClient> _httpClients = new();

    private NbpCurrencyExchangeRateClient CreateSut(TimeSpan? timeout = null)
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
        return new NbpCurrencyExchangeRateClient(httpClient, _urlBuilderFactory);
    }

    public void Dispose()
    {
        foreach (var httpClient in _httpClients)
            httpClient.Dispose();
        _server.Dispose();
    }
}

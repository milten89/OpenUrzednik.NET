using Microsoft.Extensions.Time.Testing;

using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.Options;
using OpenUrzednik.Nbp.UrlBuilder;

using WireMock.Server;
using WireMock.Settings;

namespace OpenUrzednik.IntegrationTests.Nbp.WireMock;

public partial class DefaultNbpGoldPriceClientWireMockTest : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start(new WireMockServerSettings() { UseSSL = true });
    private readonly DefaultNbpUrlBuilderFactory _urlBuilderFactory = new();
    private readonly List<HttpClient> _httpClients = new();

    private DefaultNbpGoldPriceClient CreateSut(TimeSpan? timeout = null)
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
        return new DefaultNbpGoldPriceClient(httpClient, _urlBuilderFactory);
    }

    public void Dispose()
    {
        foreach (var httpClient in _httpClients)
            httpClient.Dispose();
        _server.Dispose();
    }
}

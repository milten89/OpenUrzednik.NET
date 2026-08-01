using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.UrlBuilder;

namespace OpenUrzednik.Nbp.Table;

public partial class DefaultNbpExchangeRateTableClient : INbpExchangeRateTableClient
{
    private static readonly NbpJsonContext JsonContext = new();

    private readonly HttpClient _httpClient;
    private readonly INbpUrlBuilderFactory _urlBuilderFactoy;

    public DefaultNbpExchangeRateTableClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentNullException.ThrowIfNull(urlBuilderFactory, nameof(urlBuilderFactory));

        _httpClient = httpClient;
        _urlBuilderFactoy = urlBuilderFactory;
    }
}

using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.UrlBuilder;

namespace OpenUrzednik.Nbp.Table;

public partial class DefaultNbpExchangeRateTableClient : INbpExchangeRateTableClient
{
    private static readonly NbpJsonContext JsonContext = new();

    private readonly HttpClient _httpClient;
    private readonly INbpUrlBuilderFactory _urlBuilderFactory;
    private readonly TimeProvider _timeProvider;

    public DefaultNbpExchangeRateTableClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory)
        : this(httpClient, urlBuilderFactory, TimeProvider.System) { }
    
    public DefaultNbpExchangeRateTableClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentNullException.ThrowIfNull(urlBuilderFactory, nameof(urlBuilderFactory));
        ArgumentNullException.ThrowIfNull(timeProvider, nameof(timeProvider));

        _httpClient = httpClient;
        _urlBuilderFactory = urlBuilderFactory;
        _timeProvider = timeProvider;
    }
}

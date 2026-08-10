using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.UrlBuilder;

namespace OpenUrzednik.Nbp.Currency;

public partial class DefaultNbpCurrencyExchangeRateClient : INbpCurrencyExchangeRateClient
{
    private static readonly NbpJsonContext JsonContext = new();

    private readonly HttpClient _httpClient;
    private readonly INbpUrlBuilderFactory _urlBuilderFactoy;
    private readonly TimeProvider _timeProvider;

    public DefaultNbpCurrencyExchangeRateClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory)
        : this(httpClient, urlBuilderFactory, TimeProvider.System) { }

    public DefaultNbpCurrencyExchangeRateClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentNullException.ThrowIfNull(urlBuilderFactory, nameof(urlBuilderFactory));
        ArgumentNullException.ThrowIfNull(timeProvider, nameof(timeProvider));

        _httpClient = httpClient;
        _urlBuilderFactoy = urlBuilderFactory;
        _timeProvider = timeProvider;
    }
}

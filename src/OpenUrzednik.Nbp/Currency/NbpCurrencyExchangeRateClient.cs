using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Telemetry;
using OpenUrzednik.Nbp.UrlBuilder;

namespace OpenUrzednik.Nbp.Currency;

public partial class NbpCurrencyExchangeRateClient : INbpCurrencyExchangeRateClient
{
    private static readonly NbpJsonContext JsonContext = new();

    private readonly HttpClient _httpClient;
    private readonly INbpUrlBuilderFactory _urlBuilderFactory;
    private readonly TimeProvider _timeProvider;
    private readonly NbpTelemetryProvider _telemetryProvider;

    public NbpCurrencyExchangeRateClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory,
                                                IOpenUrzednikLogger? logger = null, IOpenUrzednikTraceSource? traceSource = null)
        : this(httpClient, urlBuilderFactory, TimeProvider.System, logger, traceSource) { }

    public NbpCurrencyExchangeRateClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory, TimeProvider timeProvider,
                                                IOpenUrzednikLogger? logger = null, IOpenUrzednikTraceSource? traceSource = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(urlBuilderFactory);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _httpClient = httpClient;
        _urlBuilderFactory = urlBuilderFactory;
        _timeProvider = timeProvider;
        _telemetryProvider = new NbpTelemetryProvider(logger, traceSource);
    }
}

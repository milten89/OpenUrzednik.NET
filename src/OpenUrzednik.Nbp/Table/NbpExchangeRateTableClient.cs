using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Telemetry;
using OpenUrzednik.Nbp.UrlBuilder;

namespace OpenUrzednik.Nbp.Table;

/// <inheritdoc cref="INbpExchangeRateTableClient"/>
public partial class NbpExchangeRateTableClient : INbpExchangeRateTableClient
{
    private static readonly NbpJsonContext JsonContext = new();

    private readonly HttpClient _httpClient;
    private readonly INbpUrlBuilderFactory _urlBuilderFactory;
    private readonly TimeProvider _timeProvider;
    private readonly NbpTelemetryProvider _telemetryProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NbpExchangeRateTableClient"/> class using the system clock.
    /// </summary>
    /// <param name="httpClient">HTTP client configured for the NBP API, e.g. with <see cref="OpenUrzednik.Nbp.Extensions.HttpClientExtensions.ConfigureForNbpApi"/>.</param>
    /// <param name="urlBuilderFactory">Builds the NBP request paths.</param>
    /// <param name="logger">Logger; <see langword="null"/> disables logging.</param>
    /// <param name="traceSource">Trace source for spans; <see langword="null"/> disables tracing.</param>
    public NbpExchangeRateTableClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory,
                                             IOpenUrzednikLogger? logger = null, IOpenUrzednikTraceSource? traceSource = null)
        : this(httpClient, urlBuilderFactory, TimeProvider.System, logger, traceSource) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NbpExchangeRateTableClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client configured for the NBP API, e.g. with <see cref="OpenUrzednik.Nbp.Extensions.HttpClientExtensions.ConfigureForNbpApi"/>.</param>
    /// <param name="urlBuilderFactory">Builds the NBP request paths.</param>
    /// <param name="timeProvider">Clock used for "today" (Europe/Warsaw date) and <c>Retry-After</c> dates.</param>
    /// <param name="logger">Logger; <see langword="null"/> disables logging.</param>
    /// <param name="traceSource">Trace source for spans; <see langword="null"/> disables tracing.</param>
    public NbpExchangeRateTableClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory, TimeProvider timeProvider,
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

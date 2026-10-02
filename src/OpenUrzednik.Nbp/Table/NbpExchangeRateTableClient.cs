using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Options;
using OpenUrzednik.Nbp.UrlBuilder;

namespace OpenUrzednik.Nbp.Table;

/// <inheritdoc cref="INbpExchangeRateTableClient"/>
public partial class NbpExchangeRateTableClient : INbpExchangeRateTableClient
{
    private static readonly NbpJsonContext JsonContext = new();

    private readonly RestRequestExecutor _connection;
    private readonly INbpUrlBuilderFactory _urlBuilderFactory;
    private readonly TimeProvider _timeProvider;
    private readonly OpenUrzednikTelemetry _telemetryProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NbpExchangeRateTableClient"/> class with the default settings:
    /// the public NBP API (or <see cref="HttpClient.BaseAddress"/>, when set) and the <see cref="HttpClient"/>'s own timeout.
    /// </summary>
    /// <param name="httpClient">HTTP client used for the requests. It isn't changed, so it can be shared with other code.</param>
    public NbpExchangeRateTableClient(HttpClient httpClient)
        : this(httpClient, options: null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NbpExchangeRateTableClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client used for the requests. It isn't changed, so it can be shared with other code.</param>
    /// <param name="options">Base URL and timeout; <see langword="null"/> uses the defaults (see <see cref="NbpOptions"/>).</param>
    /// <param name="urlBuilderFactory">Builds the request paths; <see langword="null"/> uses <see cref="NbpUrlBuilderFactory"/>.
    /// Replace it to change paths or query parameters, e.g. for a gateway.</param>
    /// <param name="timeProvider">Clock used for "today" (Europe/Warsaw date) and <c>Retry-After</c> dates; <see langword="null"/> uses <see cref="TimeProvider.System"/>.</param>
    /// <param name="logger">Logger; <see langword="null"/> disables logging.</param>
    /// <param name="traceSource">Trace source for spans; <see langword="null"/> disables tracing.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="options"/> or the <see cref="HttpClient.BaseAddress"/> is invalid (not an absolute <c>https</c> URL, or a non-positive timeout).</exception>
    public NbpExchangeRateTableClient(HttpClient httpClient, NbpOptions? options = null, INbpUrlBuilderFactory? urlBuilderFactory = null,
        TimeProvider? timeProvider = null, IOpenUrzednikLogger? logger = null, IOpenUrzednikTraceSource? traceSource = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _telemetryProvider = new OpenUrzednikTelemetry(logger, traceSource);
        _connection = NbpConnection.Create(httpClient, options, _telemetryProvider, _timeProvider);
        _urlBuilderFactory = urlBuilderFactory ?? new NbpUrlBuilderFactory();
    }
}

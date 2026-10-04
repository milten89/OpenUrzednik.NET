using OpenUrzednik.Core;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Options;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Gold;

/// <inheritdoc cref="INbpGoldPriceClient"/>
public class NbpGoldPriceClient : INbpGoldPriceClient
{
    private static readonly NbpJsonContext JsonContext = new();

    private readonly NbpRequestPipeline _pipeline;
    private readonly TimeProvider _timeProvider;
    private readonly INbpUrlBuilder _urlBuilder;
    private readonly OpenUrzednikTelemetry _telemetryProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NbpGoldPriceClient"/> class with the default settings:
    /// the public NBP API (or <see cref="HttpClient.BaseAddress"/>, when set) and the <see cref="HttpClient"/>'s own timeout.
    /// </summary>
    /// <param name="httpClient">HTTP client used for the requests. It isn't changed, so it can be shared with other code.</param>
    public NbpGoldPriceClient(HttpClient httpClient)
        : this(httpClient, options: null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NbpGoldPriceClient"/> class.
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
    public NbpGoldPriceClient(HttpClient httpClient, NbpOptions? options = null, INbpUrlBuilderFactory? urlBuilderFactory = null,
        TimeProvider? timeProvider = null, IOpenUrzednikLogger? logger = null, IOpenUrzednikTraceSource? traceSource = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _telemetryProvider = new OpenUrzednikTelemetry(logger, traceSource);
        _pipeline = new NbpRequestPipeline(NbpConnection.Create(httpClient, options, _telemetryProvider, _timeProvider), _telemetryProvider);
        _urlBuilder = (urlBuilderFactory ?? new NbpUrlBuilderFactory()).GetGoldBuilder();
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<GoldPrice>> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.gold.latest");

        return await _pipeline.GetFirstAsync(traceSpan, nameof(GetLatestAsync), OpenUrzednikResult.Success(),
            _urlBuilder.Latest, JsonContext.GoldPriceDtoArray, Mapper.MapToGoldPrice, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetTopCountAsync(int topCount, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.gold.top_count");
        traceSpan.SetTag("nbp.top_count", topCount);

        var validation = new TopCountValidator(nameof(topCount), topCount, TopCountValidator.MaxTopCount).Validate();

        return await _pipeline.GetAsync(traceSpan, nameof(GetTopCountAsync), validation,
            () => _urlBuilder.ForTopCount(topCount), JsonContext.GoldPriceDtoArray, Mapper.MapToGoldPrice, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<GoldPrice>> GetTodayAsync(CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.gold.today");

        return await _pipeline.GetFirstAsync(traceSpan, nameof(GetTodayAsync), OpenUrzednikResult.Success(),
            _urlBuilder.Today, JsonContext.GoldPriceDtoArray, Mapper.MapToGoldPrice, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<GoldPrice>> GetAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.gold.date");
        traceSpan.SetTag("nbp.date", date.ToIso8601String());

        var validation = new GoldDateValidator(nameof(date), date, NbpCalendar.Today(_timeProvider)).Validate();

        return await _pipeline.GetFirstAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilder.ForDate(date), JsonContext.GoldPriceDtoArray, Mapper.MapToGoldPrice, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.gold.range");
        traceSpan.SetTag("nbp.from", from.ToIso8601String());
        traceSpan.SetTag("nbp.to", to.ToIso8601String());

        var validation = new GoldDateValidator(nameof(to), to, NbpCalendar.Today(_timeProvider)).Validate()
            .And(new DateRangeValidator((from, to), DateRangeValidator.MaxRatesDateRange).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilder.ForDateRange(from, to), JsonContext.GoldPriceDtoArray, Mapper.MapToGoldPrice, cancellationToken).ConfigureAwait(false);
    }
}

using OpenUrzednik.Core;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp;

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
    /// <param name="timeProvider">Clock used for "today" (Europe/Warsaw date) and <c>Retry-After</c> dates; <see langword="null"/> uses <see cref="TimeProvider.System"/>.</param>
    /// <param name="logger">Logger; <see langword="null"/> disables logging.</param>
    /// <param name="traceSource">Trace source for spans; <see langword="null"/> uses the <see cref="NbpTelemetry.SourceName"/> <c>ActivitySource</c> on .NET 8 and later, and disables tracing on .NET Framework (netstandard2.0).</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="options"/> or the <see cref="HttpClient.BaseAddress"/> is invalid (not an absolute <c>https</c> URL, or a non-positive timeout).</exception>
    public NbpGoldPriceClient(HttpClient httpClient, NbpOptions? options = null, TimeProvider? timeProvider = null,
        IOpenUrzednikLogger? logger = null, IOpenUrzednikTraceSource? traceSource = null)
        : this(httpClient, new NbpUrlBuilderFactory(), options, timeProvider, logger, traceSource) { }

    // Tests replace the request paths through this constructor; applications can't (ADR-0007).
    internal NbpGoldPriceClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory, NbpOptions? options = null,
        TimeProvider? timeProvider = null, IOpenUrzednikLogger? logger = null, IOpenUrzednikTraceSource? traceSource = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _telemetryProvider = new OpenUrzednikTelemetry(logger, traceSource, NbpTelemetry.SourceName);
        _pipeline = new NbpRequestPipeline(NbpConnection.Create(httpClient, options, _telemetryProvider, _timeProvider), _telemetryProvider);
        _urlBuilder = urlBuilderFactory.GetGoldBuilder();
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
    public async Task<OpenUrzednikResult<GoldPrice>> GetAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var day = DateOnly.FromDateTime(date);
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.gold.date");
        traceSpan.SetTag("nbp.date", day.ToIso8601String());

        var validation = new GoldDateValidator(nameof(date), day, NbpCalendar.Today(_timeProvider)).Validate();

        return await _pipeline.GetFirstAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilder.ForDate(day), JsonContext.GoldPriceDtoArray, Mapper.MapToGoldPrice, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var fromDay = DateOnly.FromDateTime(from);
        var toDay = DateOnly.FromDateTime(to);
        using var traceSpan = _telemetryProvider.TraceSource.StartSpan("nbp.gold.range");
        traceSpan.SetTag("nbp.from", fromDay.ToIso8601String());
        traceSpan.SetTag("nbp.to", toDay.ToIso8601String());

        var validation = new GoldDateValidator(nameof(to), toDay, NbpCalendar.Today(_timeProvider)).Validate()
            .And(new DateRangeValidator((fromDay, toDay), DateRangeValidator.MaxRatesDateRange).Validate());

        return await _pipeline.GetAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilder.ForDateRange(fromDay, toDay), JsonContext.GoldPriceDtoArray, Mapper.MapToGoldPrice, cancellationToken).ConfigureAwait(false);
    }
}

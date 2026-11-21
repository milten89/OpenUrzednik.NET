using System.ComponentModel;

using OpenUrzednik.Core;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Http.Infrastructure;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Gold;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp;

/// <inheritdoc cref="INbpGoldPriceClient"/>
public sealed class NbpGoldPriceClient : INbpGoldPriceClient
{
    private static readonly NbpJsonContext JsonContext = new();

    private readonly NbpRequestPipeline _pipeline;
    private readonly TimeProvider _timeProvider;
    private readonly INbpUrlBuilder _urlBuilder;
    private readonly ClientTelemetry _telemetryProvider;

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
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="options"/> or the <see cref="HttpClient.BaseAddress"/> is invalid (not an absolute <c>https</c> URL, or a non-positive timeout).</exception>
    public NbpGoldPriceClient(HttpClient httpClient, NbpOptions? options)
        : this(httpClient, options, timeProvider: null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NbpGoldPriceClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client used for the requests. It isn't changed, so it can be shared with other code.</param>
    /// <param name="options">Base URL and timeout; <see langword="null"/> uses the defaults (see <see cref="NbpOptions"/>).</param>
    /// <param name="timeProvider">Clock used for "today" (Europe/Warsaw date), <c>Retry-After</c> dates and the duration of untraced operations in the metrics; <see langword="null"/> uses <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="options"/> or the <see cref="HttpClient.BaseAddress"/> is invalid (not an absolute <c>https</c> URL, or a non-positive timeout).</exception>
    public NbpGoldPriceClient(HttpClient httpClient, NbpOptions? options, TimeProvider? timeProvider)
        : this(httpClient, options, timeProvider, telemetry: null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NbpGoldPriceClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client used for the requests. It isn't changed, so it can be shared with other code.</param>
    /// <param name="options">Base URL and timeout; <see langword="null"/> uses the defaults (see <see cref="NbpOptions"/>).</param>
    /// <param name="timeProvider">Clock used for "today" (Europe/Warsaw date), <c>Retry-After</c> dates and the duration of untraced operations in the metrics; <see langword="null"/> uses <see cref="TimeProvider.System"/>.</param>
    /// <param name="telemetry">Logger, trace source and metrics; <see langword="null"/>, or a property left <see langword="null"/>, uses the default: no logging, and the <see cref="NbpTelemetry.SourceName"/> <c>ActivitySource</c> and <c>Meter</c> on .NET 8 and later (none on .NET Framework, netstandard2.0) (ADR-0025).</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="options"/> or the <see cref="HttpClient.BaseAddress"/> is invalid (not an absolute <c>https</c> URL, or a non-positive timeout).</exception>
    public NbpGoldPriceClient(HttpClient httpClient, NbpOptions? options, TimeProvider? timeProvider, OpenUrzednikTelemetry? telemetry)
        : this(httpClient, new NbpUrlBuilderFactory(), options, timeProvider, telemetry?.Logger, telemetry?.TraceSource, telemetry?.Metrics) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NbpGoldPriceClient"/> class with a logger and a trace source: use the constructor with
    /// <c>telemetry</c> instead. Removed in 2.0.
    /// </summary>
    /// <param name="httpClient">HTTP client used for the requests. It isn't changed, so it can be shared with other code.</param>
    /// <param name="options">Base URL and timeout; <see langword="null"/> uses the defaults (see <see cref="NbpOptions"/>).</param>
    /// <param name="timeProvider">Clock used for "today" (Europe/Warsaw date), <c>Retry-After</c> dates and the duration of untraced operations in the metrics; <see langword="null"/> uses <see cref="TimeProvider.System"/>.</param>
    /// <param name="logger">Logger; <see langword="null"/> disables logging.</param>
    /// <param name="traceSource">Trace source for spans; <see langword="null"/> uses the <see cref="NbpTelemetry.SourceName"/> <c>ActivitySource</c> on .NET 8 and later, and disables tracing on .NET Framework (netstandard2.0).</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="options"/> or the <see cref="HttpClient.BaseAddress"/> is invalid (not an absolute <c>https</c> URL, or a non-positive timeout).</exception>
    [Obsolete("Use the constructor (HttpClient, NbpOptions?, TimeProvider?, OpenUrzednikTelemetry?) or a shorter one; pass a logger or trace source in an OpenUrzednikTelemetry (ADR-0025). This constructor is removed in 2.0.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public NbpGoldPriceClient(HttpClient httpClient, NbpOptions? options = null, TimeProvider? timeProvider = null,
        IOpenUrzednikLogger? logger = null, IOpenUrzednikTraceSource? traceSource = null)
        : this(httpClient, new NbpUrlBuilderFactory(), options, timeProvider, logger, traceSource) { }

    // Tests replace the request paths through this constructor; applications can't (ADR-0026).
    internal NbpGoldPriceClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory, NbpOptions? options = null,
        TimeProvider? timeProvider = null, IOpenUrzednikLogger? logger = null, IOpenUrzednikTraceSource? traceSource = null,
        IOpenUrzednikMetrics? metrics = null)
    {
        // Stryker disable once Statement : equivalent, NbpConnection.Create throws the same ArgumentNullException for httpClient before the client uses it
        ArgumentNullException.ThrowIfNull(httpClient);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _telemetryProvider = new ClientTelemetry(logger, traceSource, NbpTelemetry.SourceName, metrics);
        _pipeline = new NbpRequestPipeline(NbpConnection.Create(httpClient, options, _telemetryProvider, _timeProvider), _telemetryProvider);
        _urlBuilder = urlBuilderFactory.GetGoldBuilder();
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<GoldPrice>> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        using var operation = _pipeline.StartOperation("nbp.gold.latest");
        var traceSpan = operation.Span;

        return operation.Complete(await _pipeline.GetFirstAsync(traceSpan, nameof(GetLatestAsync), OpenUrzednikResult.Success(),
            _urlBuilder.Latest, JsonContext.GoldPriceDtoArray, Mapper.MapToGoldPrice, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetTopCountAsync(int topCount, CancellationToken cancellationToken = default)
    {
        using var operation = _pipeline.StartOperation("nbp.gold.top_count");
        var traceSpan = operation.Span;
        traceSpan.SetTag("nbp.top_count", topCount);

        var validation = new TopCountValidator(nameof(topCount), topCount, TopCountValidator.MaxTopCount).Validate();

        return operation.Complete(await _pipeline.GetAsync(traceSpan, nameof(GetTopCountAsync), validation,
            () => _urlBuilder.ForTopCount(topCount), JsonContext.GoldPriceDtoArray, Mapper.MapToGoldPrice, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<GoldPrice>> GetTodayAsync(CancellationToken cancellationToken = default)
    {
        using var operation = _pipeline.StartOperation("nbp.gold.today");
        var traceSpan = operation.Span;

        return operation.Complete(await _pipeline.GetFirstAsync(traceSpan, nameof(GetTodayAsync), OpenUrzednikResult.Success(),
            _urlBuilder.Today, JsonContext.GoldPriceDtoArray, Mapper.MapToGoldPrice, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<GoldPrice>> GetAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var day = DateOnly.FromDateTime(date);
        using var operation = _pipeline.StartOperation("nbp.gold.date");
        var traceSpan = operation.Span;
        traceSpan.SetTagIfRecording("nbp.date", day, static d => d.ToIso8601String());

        var validation = new GoldDateValidator(nameof(date), day, WarsawCalendar.Today(_timeProvider)).Validate();

        return operation.Complete(await _pipeline.GetFirstAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilder.ForDate(day), JsonContext.GoldPriceDtoArray, Mapper.MapToGoldPrice, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var fromDay = DateOnly.FromDateTime(from);
        var toDay = DateOnly.FromDateTime(to);
        using var operation = _pipeline.StartOperation("nbp.gold.range");
        var traceSpan = operation.Span;
        traceSpan.SetTagIfRecording("nbp.from", fromDay, static d => d.ToIso8601String());
        traceSpan.SetTagIfRecording("nbp.to", toDay, static d => d.ToIso8601String());

        var validation = new GoldDateValidator(nameof(to), toDay, WarsawCalendar.Today(_timeProvider)).Validate()
            .And(new DateRangeValidator(nameof(from), nameof(to), (fromDay, toDay), DateRangeValidator.MaxRatesDateRange).Validate());

        return operation.Complete(await _pipeline.GetAsync(traceSpan, nameof(GetAsync), validation,
            () => _urlBuilder.ForDateRange(fromDay, toDay), JsonContext.GoldPriceDtoArray, Mapper.MapToGoldPrice, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<GoldPrice>> GetLastPublishedBeforeAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var day = DateOnly.FromDateTime(date);
        using var operation = _pipeline.StartOperation("nbp.gold.last_published_before");
        var traceSpan = operation.Span;
        traceSpan.SetTagIfRecording("nbp.date", day, static d => d.ToIso8601String());

        var validation = new LastPublishedBeforeDateValidator(nameof(date), day, GoldDateValidator.MinDate, WarsawCalendar.Today(_timeProvider)).Validate();

        return operation.Complete(await _pipeline.GetLastAsync(traceSpan, nameof(GetLastPublishedBeforeAsync), validation,
            () => _urlBuilder.ForDateRange(day.AddDays(-LastPublishedBeforeDateValidator.DailyWindowDays), day.AddDays(-1)), JsonContext.GoldPriceDtoArray, Mapper.MapLastToGoldPrice, cancellationToken).ConfigureAwait(false));
    }
}

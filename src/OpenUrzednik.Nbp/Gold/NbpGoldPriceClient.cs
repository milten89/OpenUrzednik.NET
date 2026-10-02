using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Options;
using OpenUrzednik.Nbp.Telemetry;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Gold;

/// <inheritdoc cref="INbpGoldPriceClient"/>
public class NbpGoldPriceClient : INbpGoldPriceClient
{
    private static readonly NbpJsonContext JsonContext = new();

    private readonly NbpConnection _connection;
    private readonly TimeProvider _timeProvider;
    private readonly INbpUrlBuilder _urlBuilder;
    private readonly NbpTelemetryProvider _telemetryProvider;

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
        _telemetryProvider = new NbpTelemetryProvider(logger, traceSource);
        _connection = NbpConnection.Create(httpClient, options, _telemetryProvider, _timeProvider);
        _urlBuilder = (urlBuilderFactory ?? new NbpUrlBuilderFactory()).GetGoldBuilder();
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<GoldPrice>> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.gold.latest");

        var requestResult = await _connection.GetAsync(_urlBuilder.Latest(), JsonContext.GoldPriceDtoArray, cancellationToken);

        switch (requestResult.IsSuccess)
        {
            case true when requestResult.Value.Length != 0:
                return NbpPayload.Map(requestResult.Value[0], Mapper.MapToGoldPrice, _telemetryProvider, traceSpan);
            case true when requestResult.Value.Length == 0:
                if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                    _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "NBP API returned empty array for {path}", "path", _urlBuilder.Latest());
                var error = new NotFoundError("NBP API returned empty array.");
                traceSpan.RecordError(error);
                return OpenUrzednikResult.Failure(error);
            default:
                traceSpan.RecordErrors(requestResult.Errors);
                return OpenUrzednikResult.Failure(requestResult.Errors);
        }
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetTopCountAsync(int topCount, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.gold.top_count");
        traceSpan.SetTag("nbp.top_count", topCount);

        var topCountValidation = new TopCountValidator(nameof(topCount), topCount).Validate();
        if (topCountValidation.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetTopCountAsync");
            traceSpan.RecordErrors(topCountValidation.Errors);
            return topCountValidation;
        }

        var requestResult = await _connection.GetAsync(_urlBuilder.ForTopCount(topCount), JsonContext.GoldPriceDtoArray, cancellationToken);

        if (requestResult.IsSuccess)
            return NbpPayload.Map(requestResult.Value, Mapper.MapToGoldPrice, _telemetryProvider, traceSpan);

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure<IReadOnlyList<GoldPrice>>(requestResult.Errors);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<GoldPrice>> GetTodayAsync(CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.gold.today");

        var requestResult = await _connection.GetAsync(_urlBuilder.Today(), JsonContext.GoldPriceDtoArray, cancellationToken);

        switch (requestResult.IsSuccess)
        {
            case true when requestResult.Value.Length != 0:
                return NbpPayload.Map(requestResult.Value[0], Mapper.MapToGoldPrice, _telemetryProvider, traceSpan);
            case true when requestResult.Value.Length == 0:

                if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                    _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "NBP API returned empty array for {path}", "path", _urlBuilder.Today());
                var error = new NotFoundError("NBP API returned empty array.");
                traceSpan.RecordError(error);
                return OpenUrzednikResult.Failure(error);
            default:
                traceSpan.RecordErrors(requestResult.Errors);
                return OpenUrzednikResult.Failure(requestResult.Errors);
        }
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<GoldPrice>> GetAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.gold.get_date");
        traceSpan.SetTag("nbp.date", date.ToIso8601String());

        var dateValidation = new GoldDateValidator(nameof(date), date, NbpCalendar.Today(_timeProvider)).Validate();
        if (dateValidation.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetAsync");
            traceSpan.RecordErrors(dateValidation.Errors);
            return dateValidation;
        }

        var requestResult = await _connection.GetAsync(_urlBuilder.ForDate(date), JsonContext.GoldPriceDtoArray, cancellationToken);

        switch (requestResult.IsSuccess)
        {
            case true when requestResult.Value.Length != 0:
                return NbpPayload.Map(requestResult.Value[0], Mapper.MapToGoldPrice, _telemetryProvider, traceSpan);
            case true when requestResult.Value.Length == 0:
                if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                    _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "NBP API returned empty array for {path}", "path", _urlBuilder.ForDate(date));
                var error = new NotFoundError("NBP API returned empty array.");
                traceSpan.RecordError(error);
                return OpenUrzednikResult.Failure(error);
            default:
                traceSpan.RecordErrors(requestResult.Errors);
                return OpenUrzednikResult.Failure(requestResult.Errors);
        }
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.gold.get_range");
        traceSpan.SetTag("nbp.from", from.ToIso8601String());
        traceSpan.SetTag("nbp.to", to.ToIso8601String());

        var toValidation = new GoldDateValidator(nameof(to), to, NbpCalendar.Today(_timeProvider)).Validate();
        var dateRangeValidation = new DateRangeValidator((from, to), DateRangeValidator.MaxRatesDateRange).Validate();
        var validationResult = toValidation.And(dateRangeValidation);
        if (validationResult.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetAsync");
            traceSpan.RecordErrors(validationResult.Errors);
            return validationResult;
        }

        var requestResult = await _connection.GetAsync(_urlBuilder.ForDateRange(from, to), JsonContext.GoldPriceDtoArray, cancellationToken);

        if (requestResult.IsSuccess)
            return NbpPayload.Map(requestResult.Value, Mapper.MapToGoldPrice, _telemetryProvider, traceSpan);

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure<IReadOnlyList<GoldPrice>>(requestResult.Errors);
    }
}

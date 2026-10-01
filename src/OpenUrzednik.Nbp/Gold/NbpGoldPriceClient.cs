using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Telemetry;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Gold;

/// <inheritdoc cref="INbpGoldPriceClient"/>
public class NbpGoldPriceClient : INbpGoldPriceClient
{
    private static readonly NbpJsonContext JsonContext = new();

    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;
    private readonly INbpUrlBuilder _urlBuilder;
    private readonly NbpTelemetryProvider _telemetryProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NbpGoldPriceClient"/> class using the system clock.
    /// </summary>
    /// <param name="httpClient">HTTP client configured for the NBP API, e.g. with <see cref="OpenUrzednik.Nbp.Extensions.HttpClientExtensions.ConfigureForNbpApi"/>.</param>
    /// <param name="urlBuilderFactory">Builds the NBP request paths.</param>
    /// <param name="logger">Logger; <see langword="null"/> disables logging.</param>
    /// <param name="traceSource">Trace source for spans; <see langword="null"/> disables tracing.</param>
    public NbpGoldPriceClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory,
                                     IOpenUrzednikLogger? logger = null, IOpenUrzednikTraceSource? traceSource = null)
        : this(httpClient, urlBuilderFactory, TimeProvider.System, logger, traceSource) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NbpGoldPriceClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client configured for the NBP API, e.g. with <see cref="OpenUrzednik.Nbp.Extensions.HttpClientExtensions.ConfigureForNbpApi"/>.</param>
    /// <param name="urlBuilderFactory">Builds the NBP request paths.</param>
    /// <param name="timeProvider">Clock used for "today" (Europe/Warsaw date) and <c>Retry-After</c> dates.</param>
    /// <param name="logger">Logger; <see langword="null"/> disables logging.</param>
    /// <param name="traceSource">Trace source for spans; <see langword="null"/> disables tracing.</param>
    public NbpGoldPriceClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory, TimeProvider timeProvider,
                                     IOpenUrzednikLogger? logger = null, IOpenUrzednikTraceSource? traceSource = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(urlBuilderFactory);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _httpClient = httpClient;
        _timeProvider = timeProvider;
        _urlBuilder = urlBuilderFactory.GetGoldBuilder();
        _telemetryProvider = new NbpTelemetryProvider(logger, traceSource);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<GoldPrice>> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.gold.latest");

        var requestResult = await _httpClient.GetNbpAsync(_urlBuilder.Latest(), JsonContext.GoldPriceDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

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

        var requestResult = await _httpClient.GetNbpAsync(_urlBuilder.ForTopCount(topCount), JsonContext.GoldPriceDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return NbpPayload.Map(requestResult.Value, Mapper.MapToGoldPrice, _telemetryProvider, traceSpan);

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure<IReadOnlyList<GoldPrice>>(requestResult.Errors);
    }

    /// <inheritdoc/>
    public async Task<OpenUrzednikResult<GoldPrice>> GetTodayAsync(CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.gold.today");

        var requestResult = await _httpClient.GetNbpAsync(_urlBuilder.Today(), JsonContext.GoldPriceDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

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

        var requestResult = await _httpClient.GetNbpAsync(_urlBuilder.ForDate(date), JsonContext.GoldPriceDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

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

        var requestResult = await _httpClient.GetNbpAsync(_urlBuilder.ForDateRange(from, to), JsonContext.GoldPriceDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return NbpPayload.Map(requestResult.Value, Mapper.MapToGoldPrice, _telemetryProvider, traceSpan);

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure<IReadOnlyList<GoldPrice>>(requestResult.Errors);
    }
}

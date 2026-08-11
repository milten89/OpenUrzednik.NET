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

public class DefaultNbpGoldPriceClient : INbpGoldPriceClient
{
    private static readonly NbpJsonContext JsonContext = new();

    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;
    private readonly INbpUrlBuilder _urlBuilder;
    private readonly NbpTelemetryProvider _telemetryProvider;

    public DefaultNbpGoldPriceClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory,
                                     IOpenUrzednikLogger? logger = null, IOpenUrzednikTraceSource? traceSource = null)
        : this(httpClient, urlBuilderFactory, TimeProvider.System, logger, traceSource) { }

    public DefaultNbpGoldPriceClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory, TimeProvider timeProvider,
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

    public async Task<OpenUrzednikResult<GoldPrice>> GetLatestAsync(CancellationToken cancellationToken)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.gold.latest");

        var requestResult = await _httpClient.GetNbpAsync(_urlBuilder.Latest(), JsonContext.GoldPriceDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

        switch (requestResult.IsSuccess)
        {
            case true when requestResult.Value.Length != 0:
                return OpenUrzednikResult.Success(Mapper.MapToGoldPrice(requestResult.Value[0]));
            case true when requestResult.Value.Length == 0:
                var error = new NotFoundError("NPB API returned empty array.");
                traceSpan.RecordError(error);
                return OpenUrzednikResult.Failure(error);
            default:
                traceSpan.RecordErrors(requestResult.Errors);
                return OpenUrzednikResult.Failure(requestResult.Errors);
        }
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetTopCountAsync(int topCount, CancellationToken cancellationToken)
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
            return OpenUrzednikResult.Success<IReadOnlyList<GoldPrice>>(Array.AsReadOnly(Mapper.MapToGoldPrice(requestResult.Value)));

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure<IReadOnlyList<GoldPrice>>(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<GoldPrice>> GetTodayAsync(CancellationToken cancellationToken)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.gold.today");

        var requestResult = await _httpClient.GetNbpAsync(_urlBuilder.Today(), JsonContext.GoldPriceDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

        switch (requestResult.IsSuccess)
        {
            case true when requestResult.Value.Length != 0:
                return OpenUrzednikResult.Success(Mapper.MapToGoldPrice(requestResult.Value[0]));
            case true when requestResult.Value.Length == 0:
                var error = new NotFoundError("NPB API returned empty array.");
                traceSpan.RecordError(error);
                return OpenUrzednikResult.Failure(error);
            default:
                traceSpan.RecordErrors(requestResult.Errors);
                return OpenUrzednikResult.Failure(requestResult.Errors);
        }
    }

    public async Task<OpenUrzednikResult<GoldPrice>> GetAsync(DateOnly date, CancellationToken cancellationToken)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.gold.get_date");
        traceSpan.SetTag("nbp.date", date);

        var dateValidation = new GoldDateValidator(nameof(date), date).Validate();
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
                return OpenUrzednikResult.Success(Mapper.MapToGoldPrice(requestResult.Value[0]));
            case true when requestResult.Value.Length == 0:
                var error = new NotFoundError("NPB API returned empty array.");
                traceSpan.RecordError(error);
                return OpenUrzednikResult.Failure(error);
            default:
                traceSpan.RecordErrors(requestResult.Errors);
                return OpenUrzednikResult.Failure(requestResult.Errors);
        }
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.gold.get_range");
        traceSpan.SetTag("nbp.from", from);
        traceSpan.SetTag("nbp.to", to);

        var toValidation = new GoldDateValidator(nameof(to), to).Validate();
        var dateRangeValidation = new DateRangeValidator((from, to)).Validate();
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
            return OpenUrzednikResult.Success<IReadOnlyList<GoldPrice>>(Array.AsReadOnly(Mapper.MapToGoldPrice(requestResult.Value)));

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure<IReadOnlyList<GoldPrice>>(requestResult.Errors);
    }
}

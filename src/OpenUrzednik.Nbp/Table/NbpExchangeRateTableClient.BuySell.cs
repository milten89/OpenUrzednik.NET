using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Table;

public partial class NbpExchangeRateTableClient
{
    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellLatestAsync(CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.table.buy_sell_latest");

        var urlBuilder = _urlBuilderFactory.GetTableBuilder(NbpTable.C);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Latest(), JsonContext.BuySellExchangeRateTableDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

        switch (requestResult.IsSuccess)
        {
            case true when requestResult.Value.Length != 0:
                return NbpPayload.Map(requestResult.Value[0], Mapper.MapToBuySellExchangeRateTable, _telemetryProvider, traceSpan);
            case true when requestResult.Value.Length == 0:
                if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                    _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "NBP API returned empty array for {path}", "path", urlBuilder.Latest());
                var error = new NotFoundError("NBP API returned empty array.");
                traceSpan.RecordError(error);
                return OpenUrzednikResult.Failure(error);
            default:
                traceSpan.RecordErrors(requestResult.Errors);
                return OpenUrzednikResult.Failure(requestResult.Errors);
        }
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellTopCountAsync(int topCount, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.table.buy_sell_top_count");
        traceSpan.SetTag("nbp.top_count", topCount);

        var topCountValidation = new TopCountValidator(nameof(topCount), topCount).Validate();
        if (topCountValidation.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetBuySellTopCountAsync");
            traceSpan.RecordErrors(topCountValidation.Errors);
            return topCountValidation;
        }

        var urlBuilder = _urlBuilderFactory.GetTableBuilder(NbpTable.C);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForTopCount(topCount), JsonContext.BuySellExchangeRateTableDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return NbpPayload.Map(requestResult.Value, Mapper.MapToBuySellExchangeRateTable, _telemetryProvider, traceSpan);

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure<IReadOnlyList<BuySellExchangeRateTable>>(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellTodayAsync(CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.table.buy_sell_today");

        var urlBuilder = _urlBuilderFactory.GetTableBuilder(NbpTable.C);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Today(), JsonContext.BuySellExchangeRateTableDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

        switch (requestResult.IsSuccess)
        {
            case true when requestResult.Value.Length != 0:
                return NbpPayload.Map(requestResult.Value[0], Mapper.MapToBuySellExchangeRateTable, _telemetryProvider, traceSpan);
            case true when requestResult.Value.Length == 0:
                if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                    _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "NBP API returned empty array for {path}", "path", urlBuilder.Today());
                var error = new NotFoundError("NBP API returned empty array.");
                traceSpan.RecordError(error);
                return OpenUrzednikResult.Failure(error);
            default:
                traceSpan.RecordErrors(requestResult.Errors);
                return OpenUrzednikResult.Failure(requestResult.Errors);
        }
    }

    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.table.buy_sell_date");
        traceSpan.SetTag("nbp.date", date.ToIso8601String());

        var dateValidation = new CurrencyDateValidator(nameof(date), date).Validate();
        if (dateValidation.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetBuySellAsync");
            traceSpan.RecordErrors(dateValidation.Errors);
            return dateValidation;
        }

        var urlBuilder = _urlBuilderFactory.GetTableBuilder(NbpTable.C);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDate(date), JsonContext.BuySellExchangeRateTableDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

        switch (requestResult.IsSuccess)
        {
            case true when requestResult.Value.Length != 0:
                return NbpPayload.Map(requestResult.Value[0], Mapper.MapToBuySellExchangeRateTable, _telemetryProvider, traceSpan);
            case true when requestResult.Value.Length == 0:
                if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                    _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "NBP API returned empty array for {path}", "path", urlBuilder.ForDate(date));
                var error = new NotFoundError("NBP API returned empty array.");
                traceSpan.RecordError(error);
                return OpenUrzednikResult.Failure(error);
            default:
                traceSpan.RecordErrors(requestResult.Errors);
                return OpenUrzednikResult.Failure(requestResult.Errors);
        }
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.table.buy_sell_range");
        traceSpan.SetTag("nbp.from", from.ToIso8601String());
        traceSpan.SetTag("nbp.to", to.ToIso8601String());

        var toValidation = new CurrencyDateValidator(nameof(to), to).Validate();
        var dateRangeValidation = new DateRangeValidator((from, to)).Validate();
        var validationResult = toValidation.And(dateRangeValidation);
        if (validationResult.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetBuySellAsync");
            traceSpan.RecordErrors(validationResult.Errors);
            return validationResult;
        }

        var urlBuilder = _urlBuilderFactory.GetTableBuilder(NbpTable.C);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDateRange(from, to), JsonContext.BuySellExchangeRateTableDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return NbpPayload.Map(requestResult.Value, Mapper.MapToBuySellExchangeRateTable, _telemetryProvider, traceSpan);

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure<IReadOnlyList<BuySellExchangeRateTable>>(requestResult.Errors);
    }
}

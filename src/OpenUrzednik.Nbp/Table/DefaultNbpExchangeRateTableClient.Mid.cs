using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Table;

public partial class DefaultNbpExchangeRateTableClient
{
    public async Task<OpenUrzednikResult<ExchangeRateTable>> GetLatestAsync(MidTableType table, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.table.latest");
        traceSpan.SetTag("nbp.table", table);

        var midTableValidation = new MidTableTypeValidator(nameof(table), table).Validate();
        if (midTableValidation.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetLatestAsync");
            traceSpan.RecordErrors(midTableValidation.Errors);
            return midTableValidation;
        }

        var urlBuilder = _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table));

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Latest(), JsonContext.ExchangeRateTableDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

        switch (requestResult.IsSuccess)
        {
            case true when requestResult.Value.Length != 0:
                return OpenUrzednikResult.Success(Mapper.MapToExchangeRateTable(requestResult.Value[0]));
            case true when requestResult.Value.Length == 0:
                var error = new NotFoundError("NPB API returned empty array.");
                traceSpan.RecordError(error);
                return OpenUrzednikResult.Failure(error);
            default:
                traceSpan.RecordErrors(requestResult.Errors);
                return OpenUrzednikResult.Failure(requestResult.Errors);
        }
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetTopCountAsync(MidTableType table, int topCount, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.table.top_count");
        traceSpan.SetTag("nbp.table", table);
        traceSpan.SetTag("nbp.top_count", topCount);

        var midTableValidation = new MidTableTypeValidator(nameof(table), table).Validate();
        var topCountValidation = new TopCountValidator(nameof(topCount), topCount).Validate();
        var validationResult = midTableValidation.And(topCountValidation);
        if (validationResult.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetTopCountAsync");
            traceSpan.RecordErrors(validationResult.Errors);
            return validationResult;
        }

        var urlBuilder = _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table));

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForTopCount(topCount), JsonContext.ExchangeRateTableDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return OpenUrzednikResult.Success<IReadOnlyList<ExchangeRateTable>>(Array.AsReadOnly(Mapper.MapToExchangeRateTable(requestResult.Value)));

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure<IReadOnlyList<ExchangeRateTable>>(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<ExchangeRateTable>> GetTodayAsync(MidTableType table, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.table.today");
        traceSpan.SetTag("nbp.table", table);

        var midTableValidation = new MidTableTypeValidator(nameof(table), table).Validate();
        if (midTableValidation.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetTodayAsync");
            traceSpan.RecordErrors(midTableValidation.Errors);
            return midTableValidation;
        }

        var urlBuilder = _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table));

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Today(), JsonContext.ExchangeRateTableDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

        switch (requestResult.IsSuccess)
        {
            case true when requestResult.Value.Length != 0:
                return OpenUrzednikResult.Success(Mapper.MapToExchangeRateTable(requestResult.Value[0]));
            case true when requestResult.Value.Length == 0:
                var error = new NotFoundError("NPB API returned empty array.");
                traceSpan.RecordError(error);
                return OpenUrzednikResult.Failure(error);
            default:
                traceSpan.RecordErrors(requestResult.Errors);
                return OpenUrzednikResult.Failure(requestResult.Errors);
        }
    }

    public async Task<OpenUrzednikResult<ExchangeRateTable>> GetAsync(MidTableType table, DateOnly date, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.table.get_date");
        traceSpan.SetTag("nbp.table", table);
        traceSpan.SetTag("nbp.date", date);

        var midTableValidation = new MidTableTypeValidator(nameof(table), table).Validate();
        var dateValidation = new CurrencyDateValidator(nameof(date), date).Validate();
        var validationResult = midTableValidation.And(dateValidation);
        if (validationResult.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetAsync");
            traceSpan.RecordErrors(validationResult.Errors);
            return validationResult;
        }

        var urlBuilder = _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table));

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDate(date), JsonContext.ExchangeRateTableDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

        switch (requestResult.IsSuccess)
        {
            case true when requestResult.Value.Length != 0:
                return OpenUrzednikResult.Success(Mapper.MapToExchangeRateTable(requestResult.Value[0]));
            case true when requestResult.Value.Length == 0:
                var error = new NotFoundError("NPB API returned empty array.");
                traceSpan.RecordError(error);
                return OpenUrzednikResult.Failure(error);
            default:
                traceSpan.RecordErrors(requestResult.Errors);
                return OpenUrzednikResult.Failure(requestResult.Errors);
        }
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetAsync(MidTableType table, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.table.get_range");
        traceSpan.SetTag("nbp.table", table);
        traceSpan.SetTag("nbp.from", from);
        traceSpan.SetTag("nbp.to", to);

        var midTableValidation = new MidTableTypeValidator(nameof(table), table).Validate();
        var toValidation = new CurrencyDateValidator(nameof(to), to).Validate();
        var dateRangeValidation = new DateRangeValidator((from, to)).Validate();
        var validationResult = midTableValidation.And(toValidation).And(dateRangeValidation);
        if (validationResult.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetAsync");
            traceSpan.RecordErrors(validationResult.Errors);
            return validationResult;
        }

        var urlBuilder = _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table));

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDateRange(from, to), JsonContext.ExchangeRateTableDtoArray, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return OpenUrzednikResult.Success<IReadOnlyList<ExchangeRateTable>>(Array.AsReadOnly(Mapper.MapToExchangeRateTable(requestResult.Value)));

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure<IReadOnlyList<ExchangeRateTable>>(requestResult.Errors);
    }
}

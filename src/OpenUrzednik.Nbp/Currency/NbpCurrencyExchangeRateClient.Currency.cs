using OpenUrzednik.Core;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Table;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Currency;

public partial class NbpCurrencyExchangeRateClient
{
    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetLatestAsync(string currency, TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.currency.latest");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.table", table);

        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        var tableValidation = new TableTypeValidator(nameof(table), table).Validate();
        var validationResult = currencyValidation.And(tableValidation);
        if (validationResult.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetLatestAsync");
            traceSpan.RecordErrors(validationResult.Errors);
            return validationResult;
        }

        var urlBuilder = _urlBuilderFactory.GetCurrencyBuilder(Table.Mapper.MapToNbpTable(table), currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Latest(), JsonContext.CurrencyExchangeRatesDto, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return NbpPayload.Map(requestResult.Value, Mapper.MapToCurrencyExchangeRates, _telemetryProvider, traceSpan);

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTopCountAsync(string currency, int topCount, TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.currency.top_count");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.table", table);
        traceSpan.SetTag("nbp.top_count", topCount);

        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        var tableValidation = new TableTypeValidator(nameof(table), table).Validate();
        var topCountValidation = new TopCountValidator(nameof(topCount), topCount).Validate();
        var validationResult = currencyValidation.And(tableValidation).And(topCountValidation);
        if (validationResult.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetTopCountAsync");
            traceSpan.RecordErrors(validationResult.Errors);
            return validationResult;
        }

        var urlBuilder = _urlBuilderFactory.GetCurrencyBuilder(Table.Mapper.MapToNbpTable(table), currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForTopCount(topCount), JsonContext.CurrencyExchangeRatesDto, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return NbpPayload.Map(requestResult.Value, Mapper.MapToCurrencyExchangeRates, _telemetryProvider, traceSpan);

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTodayAsync(string currency, TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.currency.today");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.table", table);

        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        var tableValidation = new TableTypeValidator(nameof(table), table).Validate();
        var validationResult = currencyValidation.And(tableValidation);
        if (validationResult.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetTodayAsync");
            traceSpan.RecordErrors(validationResult.Errors);
            return validationResult;
        }

        var urlBuilder = _urlBuilderFactory.GetCurrencyBuilder(Table.Mapper.MapToNbpTable(table), currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Today(), JsonContext.CurrencyExchangeRatesDto, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return NbpPayload.Map(requestResult.Value, Mapper.MapToCurrencyExchangeRates, _telemetryProvider, traceSpan);

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateOnly date, TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.currency.get_date");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.table", table);
        traceSpan.SetTag("nbp.date", date.ToIso8601String());

        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        var tableValidation = new TableTypeValidator(nameof(table), table).Validate();
        var dateValidation = new CurrencyDateValidator(nameof(date), date).Validate();
        var validationResult = currencyValidation.And(tableValidation).And(dateValidation);
        if (validationResult.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetAsync");
            traceSpan.RecordErrors(validationResult.Errors);
            return validationResult;
        }

        var urlBuilder = _urlBuilderFactory.GetCurrencyBuilder(Table.Mapper.MapToNbpTable(table), currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDate(date), JsonContext.CurrencyExchangeRatesDto, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return NbpPayload.Map(requestResult.Value, Mapper.MapToCurrencyExchangeRates, _telemetryProvider, traceSpan);

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateOnly from, DateOnly to, TableType table = TableType.A, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.currency.get_range");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.table", table);
        traceSpan.SetTag("nbp.from", from.ToIso8601String());
        traceSpan.SetTag("nbp.to", to.ToIso8601String());

        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        var tableValidation = new TableTypeValidator(nameof(table), table).Validate();
        var toValidation = new CurrencyDateValidator(nameof(to), to).Validate();
        var dateRangeValidation = new DateRangeValidator((from, to)).Validate();
        var validationResult = currencyValidation.And(tableValidation).And(toValidation).And(dateRangeValidation);
        if (validationResult.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetAsync");
            traceSpan.RecordErrors(validationResult.Errors);
            return validationResult;
        }

        var urlBuilder = _urlBuilderFactory.GetCurrencyBuilder(Table.Mapper.MapToNbpTable(table), currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDateRange(from, to), JsonContext.CurrencyExchangeRatesDto, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return NbpPayload.Map(requestResult.Value, Mapper.MapToCurrencyExchangeRates, _telemetryProvider, traceSpan);

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure(requestResult.Errors);
    }
}

using OpenUrzednik.Core;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Core.Telemetry;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Currency;

public partial class NbpCurrencyExchangeRateClient
{
    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetLatestAsync(string currency, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.currency.latest");
        traceSpan.SetTag("nbp.currency", currency);

        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        if (currencyValidation.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetLatestAsync");
            traceSpan.RecordErrors(currencyValidation.Errors);
            return currencyValidation;
        }

        var urlBuilder = _urlBuilderFactory.GetCurrencyBuilder(NbpTable.A, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Latest(), JsonContext.CurrencyExchangeRatesDto, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return OpenUrzednikResult.Success(Mapper.MapToCurrencyExchangeRates(requestResult.Value));

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTopCountAsync(string currency, int topCount, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.currency.top_count");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.top_count", topCount);

        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        var topCountValidation = new TopCountValidator(nameof(topCount), topCount).Validate();
        var validationResult = currencyValidation.And(topCountValidation);
        if (validationResult.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetTopCountAsync");
            traceSpan.RecordErrors(validationResult.Errors);
            return validationResult;
        }

        var urlBuilder = _urlBuilderFactory.GetCurrencyBuilder(NbpTable.A, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForTopCount(topCount), JsonContext.CurrencyExchangeRatesDto, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return OpenUrzednikResult.Success(Mapper.MapToCurrencyExchangeRates(requestResult.Value));

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTodayAsync(string currency, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.currency.today");
        traceSpan.SetTag("nbp.currency", currency);

        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        if (currencyValidation.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetTodayAsync");
            traceSpan.RecordErrors(currencyValidation.Errors);
            return currencyValidation;
        }

        var urlBuilder = _urlBuilderFactory.GetCurrencyBuilder(NbpTable.A, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Today(), JsonContext.CurrencyExchangeRatesDto, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return OpenUrzednikResult.Success(Mapper.MapToCurrencyExchangeRates(requestResult.Value));

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateOnly date, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.currency.get_date");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.date", date);

        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        var dateValidation = new CurrencyDateValidator(nameof(date), date).Validate();
        var validationResult = currencyValidation.And(dateValidation);
        if (validationResult.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetAsync");
            traceSpan.RecordErrors(validationResult.Errors);
            return validationResult;
        }

        var urlBuilder = _urlBuilderFactory.GetCurrencyBuilder(NbpTable.A, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDate(date), JsonContext.CurrencyExchangeRatesDto, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return OpenUrzednikResult.Success(Mapper.MapToCurrencyExchangeRates(requestResult.Value));

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        using var traceSpan = _telemetryProvider.Tracer.StartSpan("nbp.currency.get_range");
        traceSpan.SetTag("nbp.currency", currency);
        traceSpan.SetTag("nbp.from", from);
        traceSpan.SetTag("nbp.to", to);

        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        var toValidation = new CurrencyDateValidator(nameof(to), to).Validate();
        var dateRangeValidation = new DateRangeValidator((from, to)).Validate();
        var validationResult = currencyValidation.And(toValidation).And(dateRangeValidation);
        if (validationResult.IsFailure)
        {
            if (_telemetryProvider.Logger.IsEnabled(OpenUrzednikLogLevel.Debug))
                _telemetryProvider.Logger.Log(OpenUrzednikLogLevel.Debug, null, "Validation failed for GetAsync");
            traceSpan.RecordErrors(validationResult.Errors);
            return validationResult;
        }

        var urlBuilder = _urlBuilderFactory.GetCurrencyBuilder(NbpTable.A, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDateRange(from, to), JsonContext.CurrencyExchangeRatesDto, _telemetryProvider, _timeProvider, cancellationToken);

        if (requestResult.IsSuccess)
            return OpenUrzednikResult.Success(Mapper.MapToCurrencyExchangeRates(requestResult.Value));

        traceSpan.RecordErrors(requestResult.Errors);
        return OpenUrzednikResult.Failure(requestResult.Errors);
    }
}

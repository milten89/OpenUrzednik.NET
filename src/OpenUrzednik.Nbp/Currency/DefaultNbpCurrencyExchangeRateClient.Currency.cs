using OpenUrzednik.Core;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Currency;

public partial class DefaultNbpCurrencyExchangeRateClient
{
    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetLatestAsync(string currency, CancellationToken cancellationToken = default)
    {
        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        if (currencyValidation.IsFailure)
            return currencyValidation;
        
        var urlBuilder = _urlBuilderFactoy.GetCurrencyBuilder(NbpTable.A, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Latest(), JsonContext.CurrencyExchangeRatesDto, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToCurrencyExchangeRates(requestResult.Value))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }
    
    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTopCountAsync(string currency, int topCount, CancellationToken cancellationToken = default)
    {
        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        var topCountValidation = new TopCountValidator(nameof(topCount), topCount).Validate();
        var validationResult = currencyValidation.And(topCountValidation);
        if (validationResult.IsFailure)
            return validationResult;
        
        var urlBuilder = _urlBuilderFactoy.GetCurrencyBuilder(NbpTable.A, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForTopCount(topCount), JsonContext.CurrencyExchangeRatesDto, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToCurrencyExchangeRates(requestResult.Value))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTodayAsync(string currency, CancellationToken cancellationToken = default)
    {
        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        if (currencyValidation.IsFailure)
            return currencyValidation;
        
        var urlBuilder = _urlBuilderFactoy.GetCurrencyBuilder(NbpTable.A, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Today(), JsonContext.CurrencyExchangeRatesDto, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToCurrencyExchangeRates(requestResult.Value))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }
    
    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateOnly date, CancellationToken cancellationToken = default)
    {
        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        var dateValidation = new CurrencyDateValidator(nameof(date), date).Validate();
        var validationResult = currencyValidation.And(dateValidation);
        if (validationResult.IsFailure)
            return validationResult;
        
        var urlBuilder = _urlBuilderFactoy.GetCurrencyBuilder(NbpTable.B, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDate(date), JsonContext.CurrencyExchangeRatesDto, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToCurrencyExchangeRates(requestResult.Value))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        var toValidation = new CurrencyDateValidator(nameof(to), to).Validate();
        var dateRangeValidation = new DateRangeValidator((from, to)).Validate();
        var validationResult = currencyValidation.And(toValidation).And(dateRangeValidation);
        if (validationResult.IsFailure)
            return validationResult;
        
        var urlBuilder = _urlBuilderFactoy.GetCurrencyBuilder(NbpTable.C, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDateRange(from, to), JsonContext.CurrencyExchangeRatesDto, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToCurrencyExchangeRates(requestResult.Value))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }
}

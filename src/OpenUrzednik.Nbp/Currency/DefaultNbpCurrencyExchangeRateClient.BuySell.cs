using OpenUrzednik.Core;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Currency;

public partial class DefaultNbpCurrencyExchangeRateClient
{
    public async Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellLatestAsync(string currency, CancellationToken cancellationToken = default)
    {
        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        if (currencyValidation.IsFailure)
            return currencyValidation;
        
        var urlBuilder = _urlBuilderFactoy.GetCurrencyBuilder(NbpTable.C, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Latest(), JsonContext.BuySellCurrencyExchangeRatesDto, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToBuySellExchangeRates(requestResult.Value))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellTopCountAsync(string currency, int topCount, CancellationToken cancellationToken = default)
    {
        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        var topCountValidation = new TopCountValidator(nameof(topCount), topCount).Validate();
        var validationResult = currencyValidation.And(topCountValidation);
        if (validationResult.IsFailure)
            return validationResult;
        
        var urlBuilder = _urlBuilderFactoy.GetCurrencyBuilder(NbpTable.C, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForTopCount(topCount), JsonContext.BuySellCurrencyExchangeRatesDto, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToBuySellExchangeRates(requestResult.Value))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellTodayAsync(string currency, CancellationToken cancellationToken = default)
    {
        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        if (currencyValidation.IsFailure)
            return currencyValidation;
        
        var urlBuilder = _urlBuilderFactoy.GetCurrencyBuilder(NbpTable.C, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Today(), JsonContext.BuySellCurrencyExchangeRatesDto, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToBuySellExchangeRates(requestResult.Value))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }   

    public async Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellAsync(string currency, DateOnly date, CancellationToken cancellationToken = default)
    {
        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        var dateValidation = new CurrencyDateValidatior(nameof(date), date).Validate();
        var validationResult = currencyValidation.And(dateValidation);
        if (validationResult.IsFailure)
            return validationResult;
        
        var urlBuilder = _urlBuilderFactoy.GetCurrencyBuilder(NbpTable.C, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDate(date), JsonContext.BuySellCurrencyExchangeRatesDto, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToBuySellExchangeRates(requestResult.Value))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<BuySellExchangeRates>> GetBuySellAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var currencyValidation = new Iso4217Validator(nameof(currency), currency).Validate();
        var toValidation = new CurrencyDateValidatior(nameof(to), to).Validate();
        var dateRangeValidation = new DateRangeValidator((from, to)).Validate();
        var validationResult = currencyValidation.And(toValidation).And(dateRangeValidation);
        if (validationResult.IsFailure)
            return validationResult;
        
        var urlBuilder = _urlBuilderFactoy.GetCurrencyBuilder(NbpTable.C, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDateRange(from, to), JsonContext.BuySellCurrencyExchangeRatesDto, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToBuySellExchangeRates(requestResult.Value))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }
}

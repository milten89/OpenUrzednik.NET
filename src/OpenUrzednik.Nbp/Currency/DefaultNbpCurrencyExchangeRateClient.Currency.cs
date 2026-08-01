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
        var urlBuilder = _urlBuilderFactoy.GetCurrencyBuilder(NbpTable.A, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Latest(), JsonContext.CurrencyExchangeRatesDto, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToCurrencyExchangeRates(requestResult.Value))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }
    
    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTopCountAsync(string currency, int topCount, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactoy.GetCurrencyBuilder(NbpTable.A, currency);

        var topCountValidation = new TopCountValidator(nameof(topCount), topCount).Validate();
        if (topCountValidation.IsFailure)
            return topCountValidation;

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForTopCount(topCount), JsonContext.CurrencyExchangeRatesDto, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToCurrencyExchangeRates(requestResult.Value))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetTodayAsync(string currency, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactoy.GetCurrencyBuilder(NbpTable.A, currency);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Today(), JsonContext.CurrencyExchangeRatesDto, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToCurrencyExchangeRates(requestResult.Value))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }
    
    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateOnly date, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactoy.GetCurrencyBuilder(NbpTable.B, currency);

        var dateValidation = new CurrencyDateValidatior(nameof(date), date).Validate();
        if (dateValidation.IsFailure)
            return dateValidation;

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDate(date), JsonContext.CurrencyExchangeRatesDto, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToCurrencyExchangeRates(requestResult.Value))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<CurrencyExchangeRates>> GetAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactoy.GetCurrencyBuilder(NbpTable.C, currency);

        var toValidation = new CurrencyDateValidatior(nameof(to), to).Validate();
        var dateRangeValidation = new DateRangeValidator((from, to)).Validate();
        var validationResult = toValidation.And(dateRangeValidation);
        if (validationResult.IsFailure)
            return validationResult;

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDateRange(from, to), JsonContext.CurrencyExchangeRatesDto, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToCurrencyExchangeRates(requestResult.Value))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }
}

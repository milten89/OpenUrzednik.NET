using OpenUrzednik.Core;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Table;

public partial class DefaultNbpExchangeRateTableClient
{
    public async Task<OpenUrzednikResult<ExchangeRateTable>> GetLatestAsync(MidTableType table, CancellationToken cancellationToken = default)
    {
        var midTableValidation = new MidTableTypeValidator(nameof(table), table).Validate();
        if (midTableValidation.IsFailure)
            return midTableValidation;
        
        var urlBuilder = _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table));

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Latest(), JsonContext.ExchangeRateTableDtoArray, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToExchangeRateTable(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetTopCountAsync(MidTableType table, int topCount, CancellationToken cancellationToken = default)
    {
        var midTableValidation = new MidTableTypeValidator(nameof(table), table).Validate();
        var topCountValidation = new TopCountValidator(nameof(topCount), topCount).Validate();
        var validationResult = midTableValidation.And(topCountValidation);
        if (validationResult.IsFailure)
            return validationResult;
        
        var urlBuilder = _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table));

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForTopCount(topCount), JsonContext.ExchangeRateTableDtoArray, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success<IReadOnlyList<ExchangeRateTable>>(Mapper.MapToExchangeRateTable(requestResult.Value))
            : OpenUrzednikResult.Failure<IReadOnlyList<ExchangeRateTable>>(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<ExchangeRateTable>> GetTodayAsync(MidTableType table, CancellationToken cancellationToken = default)
    {
        var midTableValidation = new MidTableTypeValidator(nameof(table), table).Validate();
        if (midTableValidation.IsFailure)
            return midTableValidation;
        
        var urlBuilder = _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table));

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Today(), JsonContext.ExchangeRateTableDtoArray, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToExchangeRateTable(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<ExchangeRateTable>> GetAsync(MidTableType table, DateOnly date, CancellationToken cancellationToken = default)
    {
        var midTableValidation = new MidTableTypeValidator(nameof(table), table).Validate();
        var dateValidation = new CurrencyDateValidator(nameof(date), date).Validate();
        var validationResult = midTableValidation.And(dateValidation);
        if (validationResult.IsFailure)
            return validationResult;
        
        var urlBuilder = _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table));

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDate(date), JsonContext.ExchangeRateTableDtoArray, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToExchangeRateTable(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetAsync(MidTableType table, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var midTableValidation = new MidTableTypeValidator(nameof(table), table).Validate();
        var toValidation = new CurrencyDateValidator(nameof(to), to).Validate();
        var dateRangeValidation = new DateRangeValidator((from, to)).Validate();
        var validationResult = midTableValidation.And(toValidation).And(dateRangeValidation);
        if (validationResult.IsFailure)
            return validationResult;
        
        var urlBuilder = _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table));

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDateRange(from, to), JsonContext.ExchangeRateTableDtoArray, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success<IReadOnlyList<ExchangeRateTable>>(Mapper.MapToExchangeRateTable(requestResult.Value))
            : OpenUrzednikResult.Failure<IReadOnlyList<ExchangeRateTable>>(requestResult.Errors);
    }
}
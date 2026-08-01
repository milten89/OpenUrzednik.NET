using OpenUrzednik.Core;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Table;

public partial class DefaultNbpExchangeRateTableClient
{
    public async Task<OpenUrzednikResult<ExchangeRateTable>> GetLatestAsync(MidTableType table, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table));

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Latest(), JsonContext.ExchangeRateTableDtoArray, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToExchangeRateTable(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetTopCountAsync(MidTableType table, int topCount, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table));

        var topCountValidation = new TopCountValidator(nameof(topCount), topCount).Validate();
        if (topCountValidation.IsFailure)
            return topCountValidation;

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForTopCount(topCount), JsonContext.ExchangeRateTableDtoArray, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success<IReadOnlyList<ExchangeRateTable>>(Mapper.MapToExchangeRateTable(requestResult.Value))
            : OpenUrzednikResult.Failure<IReadOnlyList<ExchangeRateTable>>(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<ExchangeRateTable>> GetTodayAsync(MidTableType table, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table));

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Today(), JsonContext.ExchangeRateTableDtoArray, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToExchangeRateTable(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<ExchangeRateTable>> GetAsync(MidTableType table, DateOnly date, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table));

        var dateValidation = new CurrencyDateValidatior(nameof(date), date).Validate();
        if (dateValidation.IsFailure)
            return dateValidation;

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDate(date), JsonContext.ExchangeRateTableDtoArray, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(Mapper.MapToExchangeRateTable(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetAsync(MidTableType table, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactory.GetTableBuilder(Mapper.MapToNbpTable(table));

        var toValidation = new CurrencyDateValidatior(nameof(to), to).Validate();
        var dateRangeValidation = new DateRangeValidator((from, to)).Validate();
        var validationResult = toValidation.And(dateRangeValidation);
        if (validationResult.IsFailure)
            return validationResult;

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDateRange(from, to), JsonContext.ExchangeRateTableDtoArray, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success<IReadOnlyList<ExchangeRateTable>>(Mapper.MapToExchangeRateTable(requestResult.Value))
            : OpenUrzednikResult.Failure<IReadOnlyList<ExchangeRateTable>>(requestResult.Errors);
    }
}
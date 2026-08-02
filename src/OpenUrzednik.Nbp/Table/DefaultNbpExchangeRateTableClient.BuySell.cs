using OpenUrzednik.Core;
using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Table;

public partial class DefaultNbpExchangeRateTableClient
{
    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellLatestAsync(CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactory.GetTableBuilder(NbpTable.C);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Latest(), JsonContext.BuySellExchangeRateTableDtoArray, _timeProvider, cancellationToken);
        if (requestResult.IsFailure)
            return OpenUrzednikResult.Failure(requestResult.Errors);
        return requestResult.Value.Length != 0
            ? OpenUrzednikResult.Success(Mapper.MapToBuySellExchangeRateTable(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(new NotFoundError("NPB API returned empty array."));
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellTopCountAsync(int topCount, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactory.GetTableBuilder(NbpTable.C);

        var topCountValidation = new TopCountValidator(nameof(topCount), topCount).Validate();
        if (topCountValidation.IsFailure)
            return topCountValidation;

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForTopCount(topCount), JsonContext.BuySellExchangeRateTableDtoArray, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success<IReadOnlyList<BuySellExchangeRateTable>>(Mapper.MapToBuySellExchangeRateTable(requestResult.Value))
            : OpenUrzednikResult.Failure<IReadOnlyList<BuySellExchangeRateTable>>(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellTodayAsync(CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactory.GetTableBuilder(NbpTable.C);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Today(), JsonContext.BuySellExchangeRateTableDtoArray, _timeProvider, cancellationToken);
        if (requestResult.IsFailure)
            return OpenUrzednikResult.Failure(requestResult.Errors);
        return requestResult.Value.Length != 0
            ? OpenUrzednikResult.Success(Mapper.MapToBuySellExchangeRateTable(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(new NotFoundError("NPB API returned empty array."));
    }

    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactory.GetTableBuilder(NbpTable.C);

        var dateValidation = new CurrencyDateValidator(nameof(date), date).Validate();
        if (dateValidation.IsFailure)
            return dateValidation;

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDate(date), JsonContext.BuySellExchangeRateTableDtoArray, _timeProvider, cancellationToken);
        if (requestResult.IsFailure)
            return OpenUrzednikResult.Failure(requestResult.Errors);
        return requestResult.Value.Length != 0
            ? OpenUrzednikResult.Success(Mapper.MapToBuySellExchangeRateTable(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(new NotFoundError("NPB API returned empty array."));
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactory.GetTableBuilder(NbpTable.C);

        var toValidation = new CurrencyDateValidator(nameof(to), to).Validate();
        var dateRangeValidation = new DateRangeValidator((from, to)).Validate();
        var validationResult = toValidation.And(dateRangeValidation);
        if (validationResult.IsFailure)
            return validationResult;

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDateRange(from, to), JsonContext.BuySellExchangeRateTableDtoArray, _timeProvider, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success<IReadOnlyList<BuySellExchangeRateTable>>(Mapper.MapToBuySellExchangeRateTable(requestResult.Value))
            : OpenUrzednikResult.Failure<IReadOnlyList<BuySellExchangeRateTable>>(requestResult.Errors);
    }
}

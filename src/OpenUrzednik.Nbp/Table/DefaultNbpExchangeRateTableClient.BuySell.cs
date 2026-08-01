using OpenUrzednik.Core;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Table;

public partial class DefaultNbpExchangeRateTableClient
{
    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellLatestAsync(CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactoy.GetTableBuilder(NbpTable.C);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Latest(), JsonContext.BuySellExchangeRateTableDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(MapToBuySellExchangeRateTable(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellTopCountAsync(int topCount, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactoy.GetTableBuilder(NbpTable.C);

        var topCountValidation = new TopCountValidator(nameof(topCount), topCount).Validate();
        if (topCountValidation.IsFailure)
            return topCountValidation;

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForTopCount(topCount), JsonContext.BuySellExchangeRateTableDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success<IReadOnlyList<BuySellExchangeRateTable>>(MapToBuySellExchangeRateTable(requestResult.Value))
            : OpenUrzednikResult.Failure<IReadOnlyList<BuySellExchangeRateTable>>(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellTodayAsync(CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactoy.GetTableBuilder(NbpTable.C);

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Today(), JsonContext.BuySellExchangeRateTableDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(MapToBuySellExchangeRateTable(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<BuySellExchangeRateTable>> GetBuySellAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactoy.GetTableBuilder(NbpTable.C);

        var dateValidation = new CurrencyDateValidatior(nameof(date), date).Validate();
        if (dateValidation.IsFailure)
            return dateValidation;

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDate(date), JsonContext.BuySellExchangeRateTableDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(MapToBuySellExchangeRateTable(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<BuySellExchangeRateTable>>> GetBuySellAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactoy.GetTableBuilder(NbpTable.C);

        var toValidation = new CurrencyDateValidatior(nameof(to), to).Validate();
        var dateRangeValidation = new DateRangeValidator((from, to)).Validate();
        var validationResult = toValidation.And(dateRangeValidation);
        if (validationResult.IsFailure)
            return validationResult;

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDateRange(from, to), JsonContext.BuySellExchangeRateTableDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success<IReadOnlyList<BuySellExchangeRateTable>>(MapToBuySellExchangeRateTable(requestResult.Value))
            : OpenUrzednikResult.Failure<IReadOnlyList<BuySellExchangeRateTable>>(requestResult.Errors);
    }

    private static BuySellExchangeRateTable[] MapToBuySellExchangeRateTable(BuySellExchangeRateTableDto[] dto)
    {
        var tables = new BuySellExchangeRateTable[dto.Length];
        for (int i = 0; i < tables.Length; i++)
            tables[i] = MapToBuySellExchangeRateTable(dto[i]);

        return tables;
    }

    private static BuySellExchangeRateTable MapToBuySellExchangeRateTable(BuySellExchangeRateTableDto dto)
    {
        var rates = new BuySellExchangeRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToBuySellExchangeRateTable(dto.Rates[i]);

        return new(dto.TableId, dto.TraidingDate, dto.PublicationDate, Array.AsReadOnly(rates));
    }

    private static BuySellExchangeRate MapToBuySellExchangeRateTable(BuySellExchangeRateDto dto)
        => new(dto.CurrencyName, dto.CurrencyCode, dto.Buy, dto.Sell);
}

using OpenUrzednik.Core;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Table;

public partial class DefaultNbpExchangeRateTableClient
{
    public async Task<OpenUrzednikResult<ExchangeRateTable>> GetLatestAsync(MidTableType table, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactoy.GetTableBuilder(MapToNbpTable(table));

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Latest(), JsonContext.ExchangeRateTableDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(MapToExchangeRateTable(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetTopCountAsync(MidTableType table, int topCount, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactoy.GetTableBuilder(MapToNbpTable(table));

        var topCountValidation = new TopCountValidator(nameof(topCount), topCount).Validate();
        if (topCountValidation.IsFailure)
            return topCountValidation;

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForTopCount(topCount), JsonContext.ExchangeRateTableDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success<IReadOnlyList<ExchangeRateTable>>(MapToExchangeRateTable(requestResult.Value))
            : OpenUrzednikResult.Failure<IReadOnlyList<ExchangeRateTable>>(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<ExchangeRateTable>> GetTodayAsync(MidTableType table, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactoy.GetTableBuilder(MapToNbpTable(table));

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.Today(), JsonContext.ExchangeRateTableDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(MapToExchangeRateTable(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<ExchangeRateTable>> GetAsync(MidTableType table, DateOnly date, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactoy.GetTableBuilder(MapToNbpTable(table));

        var dateValidation = new CurrencyDateValidatior(nameof(date), date).Validate();
        if (dateValidation.IsFailure)
            return dateValidation;

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDate(date), JsonContext.ExchangeRateTableDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(MapToExchangeRateTable(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<ExchangeRateTable>>> GetAsync(MidTableType table, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var urlBuilder = _urlBuilderFactoy.GetTableBuilder(MapToNbpTable(table));

        var toValidation = new CurrencyDateValidatior(nameof(to), to).Validate();
        var dateRangeValidation = new DateRangeValidator((from, to)).Validate();
        var validationResult = toValidation.And(dateRangeValidation);
        if (validationResult.IsFailure)
            return validationResult;

        var requestResult = await _httpClient.GetNbpAsync(urlBuilder.ForDateRange(from, to), JsonContext.ExchangeRateTableDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success<IReadOnlyList<ExchangeRateTable>>(MapToExchangeRateTable(requestResult.Value))
            : OpenUrzednikResult.Failure<IReadOnlyList<ExchangeRateTable>>(requestResult.Errors);
    }

    private static NbpTable MapToNbpTable(MidTableType table)
    {
        return table switch
        {
            MidTableType.A => NbpTable.A,
            MidTableType.B => NbpTable.B,
            _ => throw new ArgumentException($"'{nameof(table)}' has value not defined by {nameof(MidTableType)}."),
        };
    }

    private static ExchangeRateTable[] MapToExchangeRateTable(ExchangeRateTableDto[] dto)
    {
        var tables = new ExchangeRateTable[dto.Length];
        for (int i = 0; i < tables.Length; i++)
            tables[i] = MapToExchangeRateTable(dto[i]);

        return tables;
    }

    private static ExchangeRateTable MapToExchangeRateTable(ExchangeRateTableDto dto)
    {
        var rates = new ExchangeRate[dto.Rates.Length];
        for (int i = 0; i < rates.Length; i++)
            rates[i] = MapToExchangeRate(dto.Rates[i]);

        return new(dto.TableId, dto.PublicationDate, Array.AsReadOnly(rates));
    }

    private static ExchangeRate MapToExchangeRate(ExchangeRateDto dto)
        => new(dto.CurrencyName, dto.CurrencyCode, dto.Price);
}

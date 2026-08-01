using OpenUrzednik.Core;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;
using OpenUrzednik.Nbp.Validation;

namespace OpenUrzednik.Nbp.Gold;

public class DefaultNbpGoldPriceClient : INbpGoldPriceClient
{
    private static readonly NbpJsonContext JsonContext = new();

    private readonly HttpClient _httpClient;
    private readonly INbpUrlBuilder _urlBuilder;

    public DefaultNbpGoldPriceClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentNullException.ThrowIfNull(urlBuilderFactory, nameof(urlBuilderFactory));

        _httpClient = httpClient;
        _urlBuilder = urlBuilderFactory.GetGoldBuilder();
    }

    public async Task<OpenUrzednikResult<GoldPrice>> GetLatestAsync(CancellationToken cancellationToken )
    {        
        var requestResult = await _httpClient.GetNbpAsync(_urlBuilder.Latest(), JsonContext.GoldPriceDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(MapToGoldPrice(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetTopCountAsync(int topCount, CancellationToken cancellationToken )
    {
        var topCountValidation = new TopCountValidator(nameof(topCount), topCount).Validate();
        if (topCountValidation.IsFailure)
            return topCountValidation;

        var requestResult = await _httpClient.GetNbpAsync(_urlBuilder.ForTopCount(topCount), JsonContext.GoldPriceDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success<IReadOnlyList<GoldPrice>>(Array.AsReadOnly(MapToGoldPrice(requestResult.Value)))
            : OpenUrzednikResult.Failure<IReadOnlyList<GoldPrice>>(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<GoldPrice>> GetTodayAsync(CancellationToken cancellationToken )
    {
        var requestResult = await _httpClient.GetNbpAsync(_urlBuilder.Today(), JsonContext.GoldPriceDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(MapToGoldPrice(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<GoldPrice>> GetAsync(DateOnly date, CancellationToken cancellationToken )
    {
        var dateValidation = new GoldDateValidator(nameof(date), date).Validate();
        if (dateValidation.IsFailure)
            return dateValidation;

        var requestResult = await _httpClient.GetNbpAsync(_urlBuilder.ForDate(date), JsonContext.GoldPriceDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(MapToGoldPrice(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken )
    {
        var toValidation = new GoldDateValidator(nameof(to), to).Validate();
        var dateRangeValidation = new DateRangeValidator((from, to)).Validate();
        var validationResult = toValidation.And(dateRangeValidation);
        if (validationResult.IsFailure)
            return validationResult;

        var requestResult = await _httpClient.GetNbpAsync(_urlBuilder.ForDateRange(from, to), JsonContext.GoldPriceDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success<IReadOnlyList<GoldPrice>>(Array.AsReadOnly(MapToGoldPrice(requestResult.Value)))
            : OpenUrzednikResult.Failure<IReadOnlyList<GoldPrice>>(requestResult.Errors);
    }

    private static GoldPrice[] MapToGoldPrice(GoldPriceDto[] dto)
    {
        var goldPrices = new GoldPrice[dto.Length];
        for (int i = 0; i < goldPrices.Length; i++)
            goldPrices[i] = MapToGoldPrice(dto[i]);

        return goldPrices;
    }

    private static GoldPrice MapToGoldPrice(GoldPriceDto goldPriceDto) 
        => new(goldPriceDto.Date, goldPriceDto.Price);
}
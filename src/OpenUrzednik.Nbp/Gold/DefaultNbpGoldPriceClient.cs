using System.Collections.ObjectModel;

using OpenUrzednik.Core;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.Extensions;
using OpenUrzednik.Nbp.UrlBuilder;

namespace OpenUrzednik.Nbp.Gold;

public class DefaultNbpGoldPriceClient : INbpGoldPriceClient
{
    private static readonly NbpJsonContext JsonContext = new();

    private readonly HttpClient _httpClient;
    private readonly INbpUrlBuilder _urlBuilder;

    public DefaultNbpGoldPriceClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFacotry)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentNullException.ThrowIfNull(urlBuilderFacotry, nameof(urlBuilderFacotry));
        _httpClient = httpClient;
        var result = urlBuilderFacotry.GetGoldBuilder();
        if (result.IsFailure)
            throw result.Errors[0].ToException();
        if (result.Value is null)
            throw new ArgumentException($"{nameof(INbpUrlBuilderFactory.GetGoldBuilder)} return null.");
        _urlBuilder = result.Value;
    }

    public async Task<OpenUrzednikResult<GoldPrice>> GetLatestAsync(CancellationToken cancellationToken )
    {
        var currentUrlResult = _urlBuilder.Latest();
        if (currentUrlResult.IsFailure)
            return OpenUrzednikResult.Failure(currentUrlResult.Errors);
        
        var requestResult = await _httpClient.GetNbpAsync(currentUrlResult.Value, JsonContext.GoldPriceDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(MapToGoldPrice(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetTopCountAsync(int topCount, CancellationToken cancellationToken )
    {
        var currentUrlResult = _urlBuilder.ForTopCount(topCount);
        if (currentUrlResult.IsFailure)
            return OpenUrzednikResult.Failure(currentUrlResult.Errors);

        var requestResult = await _httpClient.GetNbpAsync(currentUrlResult.Value, JsonContext.GoldPriceDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success<IReadOnlyList<GoldPrice>>(new ReadOnlyCollection<GoldPrice>([.. requestResult.Value.Select(MapToGoldPrice)]))
            : OpenUrzednikResult.Failure<IReadOnlyList<GoldPrice>>(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<GoldPrice>> GetTodayAsync(CancellationToken cancellationToken )
    {
        var currentUrlResult = _urlBuilder.Today();
        if (currentUrlResult.IsFailure)
            return OpenUrzednikResult.Failure(currentUrlResult.Errors);

        var requestResult = await _httpClient.GetNbpAsync(currentUrlResult.Value, JsonContext.GoldPriceDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(MapToGoldPrice(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<GoldPrice>> GetAsync(DateOnly date, CancellationToken cancellationToken )
    {
        var currentUrlResult = _urlBuilder.ForDate(date);
        if (currentUrlResult.IsFailure)
            return OpenUrzednikResult.Failure(currentUrlResult.Errors);

        var requestResult = await _httpClient.GetNbpAsync(currentUrlResult.Value, JsonContext.GoldPriceDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success(MapToGoldPrice(requestResult.Value[0]))
            : OpenUrzednikResult.Failure(requestResult.Errors);
    }

    public async Task<OpenUrzednikResult<IReadOnlyList<GoldPrice>>> GetAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken )
    {
        var currentUrlResult = _urlBuilder.ForDateRange(from, to);
        if (currentUrlResult.IsFailure)
            return OpenUrzednikResult.Failure(currentUrlResult.Errors);

        var requestResult = await _httpClient.GetNbpAsync(currentUrlResult.Value, JsonContext.GoldPriceDtoArray, cancellationToken);
        return requestResult.IsSuccess
            ? OpenUrzednikResult.Success<IReadOnlyList<GoldPrice>>(new ReadOnlyCollection<GoldPrice>([.. requestResult.Value.Select(MapToGoldPrice)]))
            : OpenUrzednikResult.Failure<IReadOnlyList<GoldPrice>>(requestResult.Errors);
    }

    private static GoldPrice MapToGoldPrice(GoldPriceDto goldPriceDto) 
        => new(goldPriceDto.Date, goldPriceDto.Price);
}
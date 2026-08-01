using OpenUrzednik.Core;
using OpenUrzednik.Nbp.Common;
using OpenUrzednik.Nbp.Dto;
using OpenUrzednik.Nbp.UrlBuilder;

namespace OpenUrzednik.Nbp.Currency;

public partial class DefaultNbpCurrencyExchangeRateClient : INbpCurrencyExchangeRateClient
{
    private static readonly NbpJsonContext JsonContext = new();

    private readonly HttpClient _httpClient;
    private readonly INbpUrlBuilderFactory _urlBuilderFactoy;

    public DefaultNbpCurrencyExchangeRateClient(HttpClient httpClient, INbpUrlBuilderFactory urlBuilderFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentNullException.ThrowIfNull(urlBuilderFactory, nameof(urlBuilderFactory));

        _httpClient = httpClient;
        _urlBuilderFactoy = urlBuilderFactory;
    }
    
    private static ExchangeRate MapToCurrencyExchangeRateDto(CurrencyExchangeRateDto dto)
        => new(dto.TableId, dto.PublicationDate, dto.Price);
}

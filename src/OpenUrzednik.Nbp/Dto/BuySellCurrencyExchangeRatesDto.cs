using System.Text.Json.Serialization;

namespace OpenUrzednik.Nbp.Dto;

internal sealed class BuySellCurrencyExchangeRatesDto
{
    [JsonPropertyName("currency")]
    public required string CurrencyName { get; init; }

    [JsonPropertyName("code")]
    public required string CurrencyCode { get; init; }
    
    [JsonPropertyName("rates")]
    public required BuySellCurrencyExchangeRateDto[] Rates { get; init; }
}
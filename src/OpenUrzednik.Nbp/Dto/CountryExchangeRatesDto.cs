using System.Text.Json.Serialization;

namespace OpenUrzednik.Nbp.Dto;

internal sealed class CountryExchangeRatesDto
{
    [JsonPropertyName("country")]
    public required string Country { get; init; }
    
    [JsonPropertyName("currency")]
    public required string CurrencyName { get; init; }
    
    [JsonPropertyName("symbol")]
    public required string CurrencySymbol { get; init; }

    [JsonPropertyName("code")]
    public required string CurrencyCode { get; init; }
    
    [JsonPropertyName("rates")]
    public required CurrencyExchangeRateDto[] Rates { get; init; }
}
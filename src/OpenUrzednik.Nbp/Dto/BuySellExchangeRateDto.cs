using System.Text.Json.Serialization;

namespace OpenUrzednik.Nbp.Dto;

internal sealed class BuySellExchangeRateDto
{
    [JsonPropertyName("currency")]
    public required string CurrencyName { get; init; }

    [JsonPropertyName("code")]
    public required string CurrencyCode { get; init; }

    [JsonPropertyName("bid")]
    public required decimal Sell { get; init; }

    [JsonPropertyName("ask")]
    public required decimal Buy { get; init; }
}
using System.Text.Json.Serialization;

namespace OpenUrzednik.Nbp.Dto;

internal sealed class BuySellExchangeRateDto
{
    // Missing in NBP answers before 2004-05-04 (up to 85/C/NBP/2004), which give "country" and "symbol" instead.
    [JsonPropertyName("currency")]
    public string? CurrencyName { get; init; }

    [JsonPropertyName("code")]
    public required string CurrencyCode { get; init; }

    [JsonPropertyName("bid")]
    public required decimal Bid { get; init; }

    [JsonPropertyName("ask")]
    public required decimal Ask { get; init; }
}

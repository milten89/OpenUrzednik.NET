using System.Text.Json.Serialization;

namespace OpenUrzednik.Nbp.Dto;

internal sealed class ExchangeRateDto
{
    [JsonPropertyName("currency")]
    public required string CurrencyName { get; init; }

    [JsonPropertyName("code")]
    public required string CurrencyCode { get; init; }

    [JsonPropertyName("mid")]
    public required decimal Price { get; init; }
}

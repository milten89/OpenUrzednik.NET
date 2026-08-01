using System.Text.Json.Serialization;

namespace OpenUrzednik.Nbp.Dto;

internal sealed class BuySellCurrencyExchangeRateDto
{
    [JsonPropertyName("no")]
    public required string TableId { get; init; }

    [JsonPropertyName("effectiveDate")]
    public required DateOnly PublicationDate { get; init; }
    
    [JsonPropertyName("bid")]
    public required decimal Sell { get; init; }

    [JsonPropertyName("ask")]
    public required decimal Buy { get; init; }
}
using System.Text.Json.Serialization;

namespace OpenUrzednik.Nbp.Dto;

internal sealed class BuySellExchangeRateTableDto
{
    [JsonPropertyName("no")]
    public required string TableId { get; init; }

    [JsonPropertyName("tradingDate")]
    public required DateOnly TradingDate { get; init; }

    [JsonPropertyName("effectiveDate")]
    public required DateOnly PublicationDate { get; init; }

    [JsonPropertyName("rates")]
    public required BuySellExchangeRateDto[] Rates { get; init; }
}

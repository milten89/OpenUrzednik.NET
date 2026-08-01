using System.Text.Json.Serialization;

namespace OpenUrzednik.Nbp.Dto;

internal sealed class ExchangeRateTableDto
{
    [JsonPropertyName("no")]
    public required string TableId { get; init; }

    [JsonPropertyName("effectiveDate")]
    public required DateOnly PublicationDate { get; init; }

    [JsonPropertyName("rates")]
    public required ExchangeRateDto[] Rates { get; init; }
}

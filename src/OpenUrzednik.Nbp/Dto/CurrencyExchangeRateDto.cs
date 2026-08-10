using System.Text.Json.Serialization;

namespace OpenUrzednik.Nbp.Dto;

internal sealed class CurrencyExchangeRateDto
{
    [JsonPropertyName("no")]
    public required string TableId { get; init; }

    [JsonPropertyName("effectiveDate")]
    public required DateOnly PublicationDate { get; init; }

    [JsonPropertyName("mid")]
    public required decimal Price { get; init; }
}

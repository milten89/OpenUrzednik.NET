using System.Text.Json.Serialization;

namespace OpenUrzednik.Nbp.Dto;

internal sealed class GoldPriceDto
{
    [JsonPropertyName("data")]
    public required DateOnly Date { get; init; }

    [JsonPropertyName("cena")]
    public required decimal Price { get; init; }
}

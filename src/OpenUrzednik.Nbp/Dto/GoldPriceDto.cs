using System.Text.Json.Serialization;

namespace OpenUrzednik.Nbp.Dto;

internal sealed record GoldPriceDto
{
    [JsonPropertyName("data")]
    public DateOnly Date { get; set; }

    [JsonPropertyName("cena")]
    public decimal Price { get; set; }
}

using System.Text.Json.Serialization;

namespace OpenUrzednik.MF.WhiteList.Dto;

/// <summary>
/// The answer of a search by one NIP or REGON (<c>EntityResponse</c> in the contract): <c>{"result": {...}}</c>.
/// </summary>
internal sealed class EntityResponseDto
{
    [JsonPropertyName("result")]
    public required EntityItemDto Result { get; init; }
}

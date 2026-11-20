using System.Text.Json.Serialization;

namespace OpenUrzednik.MF.WhiteList.Dto;

/// <summary>
/// The answer of a search by one account number (<c>EntityListResponse</c> in the contract): <c>{"result": {...}}</c>.
/// </summary>
internal sealed class EntityListResponseDto
{
    [JsonPropertyName("result")]
    public required EntityListDto Result { get; init; }
}

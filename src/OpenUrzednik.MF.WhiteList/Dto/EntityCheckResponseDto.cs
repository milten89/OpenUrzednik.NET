using System.Text.Json.Serialization;

namespace OpenUrzednik.MF.WhiteList.Dto;

/// <summary>
/// The answer of a check of an account number (<c>EntityCheckResponse</c> in the contract): <c>{"result": {...}}</c>.
/// </summary>
internal sealed class EntityCheckResponseDto
{
    [JsonPropertyName("result")]
    public required EntityCheckDto Result { get; init; }
}

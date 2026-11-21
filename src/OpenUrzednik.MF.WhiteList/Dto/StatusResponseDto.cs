using System.Text.Json.Serialization;

namespace OpenUrzednik.MF.WhiteList.Dto;

/// <summary>
/// The answer of the WL2 status service (<c>StatusResponse</c> in the contract): <c>{"result": {...}}</c>. Every field is optional:
/// the mapper names what is missing.
/// </summary>
internal sealed class StatusResponseDto
{
    [JsonPropertyName("result")]
    public StatusResultDto? Result { get; init; }
}

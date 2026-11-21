using System.Text.Json.Serialization;

namespace OpenUrzednik.MF.WhiteList.Dto;

/// <summary>
/// The answer of a batch search by NIPs, REGONs or account numbers (<c>EntryListResponse</c> in the contract): <c>{"result": {...}}</c>.
/// </summary>
internal sealed class EntryListResponseDto
{
    [JsonPropertyName("result")]
    public required EntryListDto Result { get; init; }
}

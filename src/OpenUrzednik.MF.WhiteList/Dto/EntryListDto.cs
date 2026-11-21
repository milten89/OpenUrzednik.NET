using System.Text.Json.Serialization;

namespace OpenUrzednik.MF.WhiteList.Dto;

/// <summary>
/// The <c>result</c> of a batch search (<c>EntryList</c>): one entry per identifier sent, in the order sent (duplicates included), and
/// the proof of the check. <c>requestId</c> and <c>requestDateTime</c> were in every captured answer.
/// </summary>
internal sealed class EntryListDto
{
    [JsonPropertyName("entries")]
    public EntryDto?[]? Entries { get; init; }

    [JsonPropertyName("requestId")]
    public required string RequestId { get; init; }

    // "dd-MM-yyyy HH:mm:ss", Warsaw local time without an offset.
    [JsonPropertyName("requestDateTime")]
    public required string RequestDateTime { get; init; }
}

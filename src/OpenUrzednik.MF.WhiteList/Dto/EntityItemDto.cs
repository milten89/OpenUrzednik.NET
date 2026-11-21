using System.Text.Json.Serialization;

namespace OpenUrzednik.MF.WhiteList.Dto;

/// <summary>
/// The <c>result</c> of a search by one NIP or REGON (<c>EntityItem</c>): the subject, or <see langword="null"/> when the register has
/// none, and the proof of the check. <c>requestId</c> and <c>requestDateTime</c> were in every captured answer.
/// </summary>
internal sealed class EntityItemDto
{
    [JsonPropertyName("subject")]
    public EntityDto? Subject { get; init; }

    [JsonPropertyName("requestId")]
    public required string RequestId { get; init; }

    // "dd-MM-yyyy HH:mm:ss", Warsaw local time without an offset.
    [JsonPropertyName("requestDateTime")]
    public required string RequestDateTime { get; init; }
}

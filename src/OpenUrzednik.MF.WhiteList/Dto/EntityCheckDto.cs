using System.Text.Json.Serialization;

namespace OpenUrzednik.MF.WhiteList.Dto;

/// <summary>
/// The <c>result</c> of a check (<c>EntityCheck</c>): <c>TAK</c> or <c>NIE</c> and the proof of the check. All three fields were in every
/// captured answer.
/// </summary>
internal sealed class EntityCheckDto
{
    // "TAK" (the account is on the list for the entity) or "NIE".
    [JsonPropertyName("accountAssigned")]
    public required string AccountAssigned { get; init; }

    [JsonPropertyName("requestId")]
    public required string RequestId { get; init; }

    // "dd-MM-yyyy HH:mm:ss", Warsaw local time without an offset.
    [JsonPropertyName("requestDateTime")]
    public required string RequestDateTime { get; init; }
}

using System.Text.Json.Serialization;

namespace OpenUrzednik.MF.WhiteList.Dto;

/// <summary>
/// The <c>result</c> of the WL2 status service (<c>StatusResult</c>): the <c>subject</c> with the status, also when the NIP is rejected.
/// </summary>
internal sealed class StatusResultDto
{
    [JsonPropertyName("subject")]
    public StatusSubjectDto? Subject { get; init; }
}

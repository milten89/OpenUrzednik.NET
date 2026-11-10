using System.Text.Json.Serialization;

namespace OpenUrzednik.MF.WhiteList.Dto;

/// <summary>
/// The body of an error response: <c>{"message": "...", "code": "WL-115"}</c>. Both fields are optional, because a 429 or a 5xx from a
/// proxy may come without them.
/// </summary>
internal sealed class ErrorDto
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

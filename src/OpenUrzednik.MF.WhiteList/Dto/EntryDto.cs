using System.Text.Json.Serialization;

namespace OpenUrzednik.MF.WhiteList.Dto;

/// <summary>
/// One entry of a batch search (<c>Entry</c>): the identifier as sent (the plain digits), and either its subjects (empty when the
/// register has none) or the API's <c>error</c> for that identifier (<c>{"message", "code"}</c>, e.g. <c>WL-115</c>). <c>identifier</c>
/// was in every captured entry.
/// </summary>
internal sealed class EntryDto
{
    [JsonPropertyName("identifier")]
    public required string Identifier { get; init; }

    [JsonPropertyName("subjects")]
    public EntityDto?[]? Subjects { get; init; }

    [JsonPropertyName("error")]
    public ErrorDto? Error { get; init; }
}

using System.Text.Json.Serialization;

namespace OpenUrzednik.MF.WhiteList.Dto;

/// <summary>
/// A representative, proxy (<c>authorizedClerks</c>) or partner of a subject (<c>EntityPerson</c> in the contract): every field is
/// optional. <c>pesel</c> isn't read (ADR-0023).
/// </summary>
internal sealed class EntityPersonDto
{
    [JsonPropertyName("companyName")]
    public string? CompanyName { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("nip")]
    public string? Nip { get; init; }
}

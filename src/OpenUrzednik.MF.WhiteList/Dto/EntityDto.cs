using System.Text.Json.Serialization;

namespace OpenUrzednik.MF.WhiteList.Dto;

/// <summary>
/// One subject of the register (<c>Entity</c> in the contract v1.6.2). The contract requires only <c>name</c>; every other field is
/// optional and nullable. <c>pesel</c> isn't read: the list has carried no PESEL since 2021-01-01 (ADR-0023).
/// </summary>
internal sealed class EntityDto
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("nip")]
    public string? Nip { get; init; }

    // "Czynny", "Zwolniony" or "Niezarejestrowany".
    [JsonPropertyName("statusVat")]
    public string? StatusVat { get; init; }

    [JsonPropertyName("regon")]
    public string? Regon { get; init; }

    [JsonPropertyName("krs")]
    public string? Krs { get; init; }

    [JsonPropertyName("residenceAddress")]
    public string? ResidenceAddress { get; init; }

    [JsonPropertyName("workingAddress")]
    public string? WorkingAddress { get; init; }

    [JsonPropertyName("representatives")]
    public EntityPersonDto?[]? Representatives { get; init; }

    [JsonPropertyName("authorizedClerks")]
    public EntityPersonDto?[]? AuthorizedClerks { get; init; }

    [JsonPropertyName("partners")]
    public EntityPersonDto?[]? Partners { get; init; }

    [JsonPropertyName("registrationLegalDate")]
    public DateOnly? RegistrationLegalDate { get; init; }

    [JsonPropertyName("registrationDenialDate")]
    public DateOnly? RegistrationDenialDate { get; init; }

    // The test host sends "" for an empty basis, production null.
    [JsonPropertyName("registrationDenialBasis")]
    public string? RegistrationDenialBasis { get; init; }

    [JsonPropertyName("restorationDate")]
    public DateOnly? RestorationDate { get; init; }

    [JsonPropertyName("restorationBasis")]
    public string? RestorationBasis { get; init; }

    [JsonPropertyName("removalDate")]
    public DateOnly? RemovalDate { get; init; }

    [JsonPropertyName("removalBasis")]
    public string? RemovalBasis { get; init; }

    // "Data zwolnienia SME", added in the contract v1.6.2.
    [JsonPropertyName("exemptionSmeDate")]
    public DateOnly? ExemptionSmeDate { get; init; }

    [JsonPropertyName("accountNumbers")]
    public string?[]? AccountNumbers { get; init; }

    [JsonPropertyName("hasVirtualAccounts")]
    public bool? HasVirtualAccounts { get; init; }
}

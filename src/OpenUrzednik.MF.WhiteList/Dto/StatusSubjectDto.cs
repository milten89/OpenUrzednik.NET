using System.Text.Json.Serialization;

namespace OpenUrzednik.MF.WhiteList.Dto;

/// <summary>
/// The WL2 status of one NIP (<c>StatusSubject</c>). The mapper reads only <c>statusCode</c> and the proof; <c>nip</c>, the text
/// <c>status</c> and <c>statusVat</c> are not mapped (the NIP stays out of results and the texts are the API's wording).
/// </summary>
internal sealed class StatusSubjectDto
{
    // C (active), Z (exempt), N (not registered), B (not in the VAT register), I (invalid NIP), D (date out of range), X (service inactive).
    [JsonPropertyName("statusCode")]
    public string? StatusCode { get; init; }

    // "dd-MM-yyyy": the day the status is for; today when the query had no date.
    [JsonPropertyName("requestedDate")]
    public string? RequestedDate { get; init; }

    // "dd-MM-yyyy HH:mm:ss", Warsaw local time without an offset.
    [JsonPropertyName("responseDateTime")]
    public string? ResponseDateTime { get; init; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; init; }
}

using OpenUrzednik.Core;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Dto;

namespace OpenUrzednik.MF.WhiteList.Search;

/// <summary>
/// Maps the search answers to the public models. Data the models can't take is an <see cref="InvalidPayloadException"/> (the pipeline
/// returns it as a <c>SerializationError</c>); an answer without a subject is a <see cref="WhiteListNotFoundError"/> with the proof.
/// </summary>
internal static class Mapper
{
    // The register's statusVat values.
    internal const string Active = "Czynny";
    internal const string Exempt = "Zwolniony";
    internal const string NotRegistered = "Niezarejestrowany";

    internal const string SubjectNotOnListMessage = "MF White List API has no subject on the list for the identifier on the requested date.";
    internal const string AccountNotOnListMessage = "MF White List API has no subject on the list with the account number on the requested date.";

    /// <summary>The subject of a search by NIP or REGON, or a <see cref="WhiteListNotFoundError"/> when the answer has none.</summary>
    internal static OpenUrzednikResult<SubjectSearchResult> MapToSubjectSearchResult(EntityResponseDto dto)
    {
        WhiteListPayload.EnsurePresent(dto, "result");
        var result = dto.Result;
        WhiteListPayload.EnsurePresent(result, "result");
        WhiteListPayload.EnsurePresent(result.RequestId, "requestId");
        var requestDateTime = WhiteListPayload.ParseRequestDateTime(result.RequestDateTime);

        if (result.Subject is null)
            return OpenUrzednikResult.Failure<SubjectSearchResult>(new WhiteListNotFoundError(SubjectNotOnListMessage, result.RequestId, requestDateTime));

        return OpenUrzednikResult.Success(new SubjectSearchResult(MapToSubject(result.Subject), result.RequestId, requestDateTime));
    }

    /// <summary>
    /// The subjects of a search by account number, or a <see cref="WhiteListNotFoundError"/> when the answer has none (<c>subjects: []</c>,
    /// or no list at all).
    /// </summary>
    internal static OpenUrzednikResult<SubjectsSearchResult> MapToSubjectsSearchResult(EntityListResponseDto dto)
    {
        WhiteListPayload.EnsurePresent(dto, "result");
        var result = dto.Result;
        WhiteListPayload.EnsurePresent(result, "result");
        WhiteListPayload.EnsurePresent(result.RequestId, "requestId");
        var requestDateTime = WhiteListPayload.ParseRequestDateTime(result.RequestDateTime);

        var subjects = result.Subjects;
        if (subjects is null || subjects.Length == 0)
            return OpenUrzednikResult.Failure<SubjectsSearchResult>(new WhiteListNotFoundError(AccountNotOnListMessage, result.RequestId, requestDateTime));

        var mapped = new Subject[subjects.Length];
        for (var i = 0; i < mapped.Length; i++)
        {
            var subject = subjects[i];
            WhiteListPayload.EnsurePresent(subject, "subjects[]");
            mapped[i] = MapToSubject(subject);
        }

        return OpenUrzednikResult.Success(new SubjectsSearchResult(Array.AsReadOnly(mapped), result.RequestId, requestDateTime));
    }

    /// <summary>
    /// The entries of a batch search, in the order of the answer, which keeps the order of the request; <paramref name="count"/> is the
    /// number of identifiers sent, and an answer with another number of entries is an <see cref="InvalidPayloadException"/>, so no entry
    /// is ever paired with another identifier.
    /// </summary>
    internal static BatchSearchResult MapToBatchSearchResult(EntryListResponseDto dto, int count)
    {
        WhiteListPayload.EnsurePresent(dto, "result");
        var result = dto.Result;
        WhiteListPayload.EnsurePresent(result, "result");
        WhiteListPayload.EnsurePresent(result.RequestId, "requestId");
        var requestDateTime = WhiteListPayload.ParseRequestDateTime(result.RequestDateTime);

        var entries = result.Entries;
        WhiteListPayload.EnsurePresent(entries, "entries");
        if (entries.Length != count)
            throw new InvalidPayloadException("MF White List API response has another number of 'entries' than the identifiers sent.");

        var mapped = new BatchSearchEntry[entries.Length];
        for (var i = 0; i < mapped.Length; i++)
        {
            var entry = entries[i];
            WhiteListPayload.EnsurePresent(entry, "entries[]");
            mapped[i] = MapToBatchSearchEntry(entry, result.RequestId, requestDateTime);
        }

        return new BatchSearchResult(Array.AsReadOnly(mapped), result.RequestId, requestDateTime);
    }

    /// <summary>
    /// One entry of a batch search: its subjects; a <see cref="WhiteListNotFoundError"/> with the batch's proof when it has none
    /// (<c>subjects: []</c>); or a <see cref="WhiteListQueryRejectedError"/> for its <c>error</c>, with the identifiers the message echoes
    /// masked and only a code of the documented shape kept (ADR-0023). An entry with neither is an <see cref="InvalidPayloadException"/>.
    /// </summary>
    internal static BatchSearchEntry MapToBatchSearchEntry(EntryDto dto, string requestId, DateTimeOffset requestDateTime)
    {
        WhiteListPayload.EnsurePresent(dto.Identifier, "entries[].identifier");

        if (dto.Error is not null)
        {
            var code = WhiteListErrorMapping.SanitizeCode(dto.Error.Code);
            var error = new WhiteListQueryRejectedError(
                $"MF White List API rejected the identifier{WhiteListErrorMapping.Detail(code, dto.Error.Message)}", code);
            return new BatchSearchEntry(dto.Identifier, Array.Empty<Subject>(), error);
        }

        var subjects = dto.Subjects;
        if (subjects is null)
            throw new InvalidPayloadException("MF White List API response has an entry with neither 'subjects' nor 'error'.");

        if (subjects.Length == 0)
            return new BatchSearchEntry(dto.Identifier, Array.Empty<Subject>(), new WhiteListNotFoundError(SubjectNotOnListMessage, requestId, requestDateTime));

        var mapped = new Subject[subjects.Length];
        for (var i = 0; i < mapped.Length; i++)
        {
            var subject = subjects[i];
            WhiteListPayload.EnsurePresent(subject, "entries[].subjects[]");
            mapped[i] = MapToSubject(subject);
        }

        return new BatchSearchEntry(dto.Identifier, Array.AsReadOnly(mapped), null);
    }

    internal static Subject MapToSubject(EntityDto dto)
    {
        WhiteListPayload.EnsurePresent(dto.Name, "subject.name");

        return new Subject(dto.Name)
        {
            Nip = dto.Nip,
            VatStatus = MapToVatStatus(dto.StatusVat),
            Regon = dto.Regon,
            Krs = dto.Krs,
            ResidenceAddress = dto.ResidenceAddress,
            WorkingAddress = dto.WorkingAddress,
            Representatives = MapToPersons(dto.Representatives, "representatives[]"),
            AuthorizedClerks = MapToPersons(dto.AuthorizedClerks, "authorizedClerks[]"),
            Partners = MapToPersons(dto.Partners, "partners[]"),
            RegistrationLegalDate = dto.RegistrationLegalDate?.ToPublicDate(),
            RegistrationDenialDate = dto.RegistrationDenialDate?.ToPublicDate(),
            RegistrationDenialBasis = NullIfEmpty(dto.RegistrationDenialBasis),
            RestorationDate = dto.RestorationDate?.ToPublicDate(),
            RestorationBasis = NullIfEmpty(dto.RestorationBasis),
            RemovalDate = dto.RemovalDate?.ToPublicDate(),
            RemovalBasis = NullIfEmpty(dto.RemovalBasis),
            ExemptionSmeDate = dto.ExemptionSmeDate?.ToPublicDate(),
            AccountNumbers = MapToAccountNumbers(dto.AccountNumbers),
            HasVirtualAccounts = dto.HasVirtualAccounts,
        };
    }

    /// <summary>The status; any value the contract doesn't list is an <see cref="InvalidPayloadException"/>, never a guess.</summary>
    internal static VatStatus? MapToVatStatus(string? statusVat) => statusVat switch
    {
        null => null,
        Active => VatStatus.Active,
        Exempt => VatStatus.Exempt,
        NotRegistered => VatStatus.NotRegistered,
        _ => throw new InvalidPayloadException("MF White List API response has an unknown 'statusVat'."),
    };

    private static IReadOnlyList<SubjectPerson> MapToPersons(EntityPersonDto?[]? dtos, string name)
    {
        // Nearly every subject has empty representatives, clerks and partners: no array and wrapper for them.
        if (dtos is null || dtos.Length == 0)
            return Array.Empty<SubjectPerson>();

        var persons = new SubjectPerson[dtos.Length];
        for (var i = 0; i < persons.Length; i++)
        {
            var dto = dtos[i];
            WhiteListPayload.EnsurePresent(dto, name);
            persons[i] = new SubjectPerson(dto.CompanyName, dto.FirstName, dto.LastName, dto.Nip);
        }

        return Array.AsReadOnly(persons);
    }

    private static IReadOnlyList<string> MapToAccountNumbers(string?[]? accountNumbers)
    {
        if (accountNumbers is null || accountNumbers.Length == 0)
            return Array.Empty<string>();

        var copy = new string[accountNumbers.Length];
        for (var i = 0; i < copy.Length; i++)
        {
            var accountNumber = accountNumbers[i];
            WhiteListPayload.EnsurePresent(accountNumber, "accountNumbers[]");
            copy[i] = accountNumber;
        }

        return Array.AsReadOnly(copy);
    }

    // The test host sends "" for a basis production leaves null: both are "no basis".
    private static string? NullIfEmpty(string? value) => value?.Length == 0 ? null : value;
}

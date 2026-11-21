using OpenUrzednik.Core;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Dto;

namespace OpenUrzednik.MF.WhiteList.Status;

/// <summary>
/// Maps the answer of the WL2 status service to the public model. Data the model can't take is an <see cref="InvalidPayloadException"/>
/// (the pipeline returns it as a <c>SerializationError</c>); <c>B</c> is a <see cref="WhiteListNotFoundError"/> with the proof, <c>I</c>
/// and <c>D</c> a <see cref="WhiteListQueryRejectedError"/> and <c>X</c> a <see cref="WhiteListServiceInactiveError"/>. The messages are
/// constants: they carry neither the NIP nor the API's texts.
/// </summary>
internal static class Mapper
{
    // The register's statusCode values.
    internal const string Active = "C";
    internal const string Exempt = "Z";
    internal const string NotRegistered = "N";
    internal const string NotInRegister = "B";
    internal const string InvalidNip = "I";
    internal const string DateOutOfRange = "D";
    internal const string ServiceInactive = "X";

    internal const string NotInRegisterMessage = "MF White List API has no VAT register entry for the NIP on the requested date.";
    internal const string InvalidNipMessage = "MF White List API rejected the query: invalid NIP.";
    internal const string DateOutOfRangeMessage = "MF White List API rejected the query: date out of range.";
    internal const string ServiceInactiveMessage = "MF White List API status service is inactive.";

    internal static OpenUrzednikResult<VatStatusResult> MapToVatStatusResult(StatusResponseDto dto)
    {
        WhiteListPayload.EnsurePresent(dto, "result");
        var result = dto.Result;
        WhiteListPayload.EnsurePresent(result, "result");
        var subject = result.Subject;
        WhiteListPayload.EnsurePresent(subject, "subject");
        var code = subject.StatusCode;
        WhiteListPayload.EnsurePresent(code, "statusCode");

        switch (code)
        {
            case Active:
                return Success(VatStatus.Active, subject);
            case Exempt:
                return Success(VatStatus.Exempt, subject);
            case NotRegistered:
                return Success(VatStatus.NotRegistered, subject);
            case NotInRegister:
                WhiteListPayload.EnsurePresent(subject.RequestId, "requestId");
                var answeredAt = WhiteListPayload.ParseRequestDateTime(subject.ResponseDateTime, WhiteListPayload.ResponseDateTimeField);
                return OpenUrzednikResult.Failure<VatStatusResult>(new WhiteListNotFoundError(NotInRegisterMessage, subject.RequestId, answeredAt));
            case InvalidNip:
                return OpenUrzednikResult.Failure<VatStatusResult>(new WhiteListQueryRejectedError(InvalidNipMessage, InvalidNip));
            case DateOutOfRange:
                return OpenUrzednikResult.Failure<VatStatusResult>(new WhiteListQueryRejectedError(DateOutOfRangeMessage, DateOutOfRange));
            case ServiceInactive:
                return OpenUrzednikResult.Failure<VatStatusResult>(new WhiteListServiceInactiveError(ServiceInactiveMessage, ServiceInactive));
            default:
                throw new InvalidPayloadException("MF White List API response has a 'statusCode' that isn't one of C, Z, N, B, I, D, X.");
        }
    }

    private static OpenUrzednikResult<VatStatusResult> Success(VatStatus status, StatusSubjectDto subject)
    {
        WhiteListPayload.EnsurePresent(subject.RequestId, "requestId");
        var answeredAt = WhiteListPayload.ParseRequestDateTime(subject.ResponseDateTime, WhiteListPayload.ResponseDateTimeField);
        var date = WhiteListPayload.ParseRequestedDate(subject.RequestedDate);

        return OpenUrzednikResult.Success(new VatStatusResult(status, date, subject.RequestId, answeredAt));
    }
}

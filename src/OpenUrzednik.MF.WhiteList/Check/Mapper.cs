using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Dto;

namespace OpenUrzednik.MF.WhiteList.Check;

/// <summary>
/// Maps the answer of a check to the public model. Data the model can't take is an <see cref="InvalidPayloadException"/> (the pipeline
/// returns it as a <c>SerializationError</c>); its message names the field, never the value.
/// </summary>
internal static class Mapper
{
    // The register's accountAssigned values.
    internal const string Assigned = "TAK";
    internal const string NotAssigned = "NIE";

    internal static AccountCheckResult MapToAccountCheckResult(EntityCheckResponseDto dto)
    {
        WhiteListPayload.EnsurePresent(dto, "result");
        var result = dto.Result;
        WhiteListPayload.EnsurePresent(result, "result");
        WhiteListPayload.EnsurePresent(result.AccountAssigned, "accountAssigned");
        WhiteListPayload.EnsurePresent(result.RequestId, "requestId");
        var requestDateTime = WhiteListPayload.ParseRequestDateTime(result.RequestDateTime);

        var assigned = result.AccountAssigned switch
        {
            Assigned => true,
            NotAssigned => false,
            _ => throw new InvalidPayloadException($"MF White List API response has an 'accountAssigned' that is neither '{Assigned}' nor '{NotAssigned}'."),
        };

        return new AccountCheckResult(assigned, result.RequestId, requestDateTime);
    }
}

using OpenUrzednik.MF.WhiteList.Dto;

namespace OpenUrzednik.MF.WhiteList.Tests.Search;

// Shared helpers; tests are in one file per method.
public sealed partial class MapperTest
{
    private const string RequestId = "EftiD-99alll1";
    private const string RequestDateTime = "20-11-2026 22:44:25";
    private static readonly DateTimeOffset RequestInstant = new(2026, 11, 20, 22, 44, 25, TimeSpan.FromHours(1));

    private static EntityResponseDto Response(EntityDto? subject, string requestId = RequestId, string requestDateTime = RequestDateTime)
        => new() { Result = new EntityItemDto { Subject = subject, RequestId = requestId, RequestDateTime = requestDateTime } };

    private static EntityListResponseDto ListResponse(EntityDto?[]? subjects, string requestId = RequestId, string requestDateTime = RequestDateTime)
        => new() { Result = new EntityListDto { Subjects = subjects, RequestId = requestId, RequestDateTime = requestDateTime } };

    private static EntityDto Minimal(string name = "MINISTERSTWO FINANSÓW") => new() { Name = name };
}

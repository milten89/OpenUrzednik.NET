using OpenUrzednik.Core.Errors;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Dto;
using OpenUrzednik.MF.WhiteList.Search;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Search;

public sealed partial class MapperTest
{
    [Fact]
    public void MapToSubjectSearchResult_Subject_ReturnsItWithTheProof()
    {
        // Act
        var result = Mapper.MapToSubjectSearchResult(Response(Minimal()));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new SubjectSearchResult(new Subject("MINISTERSTWO FINANSÓW"), RequestId, RequestInstant));
    }

    [Fact]
    public void MapToSubjectSearchResult_NoSubject_ReturnsNotFoundWithTheProof()
    {
        // Act
        var result = Mapper.MapToSubjectSearchResult(Response(null));

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
        error.Code.ShouldBe(NotFoundError.ErrorCode);
        error.Message.ShouldBe("MF White List API has no subject on the list for the identifier on the requested date.");
        error.RequestId.ShouldBe(RequestId);
        error.RequestDateTime.ShouldBe(RequestInstant);
        error.RequestDateTime.Offset.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public void MapToSubjectSearchResult_NullDto_ThrowsMissingResult()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToSubjectSearchResult(null!))
            .Message.ShouldBe("MF White List API response is missing 'result'.");
    }

    [Fact]
    public void MapToSubjectSearchResult_NullResult_ThrowsMissingResult()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToSubjectSearchResult(new EntityResponseDto { Result = null! }))
            .Message.ShouldBe("MF White List API response is missing 'result'.");
    }

    [Fact]
    public void MapToSubjectSearchResult_NullRequestId_ThrowsMissingRequestId()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToSubjectSearchResult(Response(null, requestId: null!)))
            .Message.ShouldBe("MF White List API response is missing 'requestId'.");
    }

    [Fact]
    public void MapToSubjectSearchResult_BadRequestDateTime_Throws()
    {
        // Act & Assert: also without a subject, the proof must be valid
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToSubjectSearchResult(Response(null, requestDateTime: "2026-11-20 22:44:25")))
            .Message.ShouldBe("MF White List API response has a 'requestDateTime' not in the format dd-MM-yyyy HH:mm:ss.");
    }
}

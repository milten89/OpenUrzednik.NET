using OpenUrzednik.Core.Errors;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Dto;
using OpenUrzednik.MF.WhiteList.Search;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Search;

public sealed partial class MapperTest
{
    [Fact]
    public void MapToSubjectsSearchResult_Subjects_ReturnsThemInOrderWithTheProof()
    {
        // Act
        var result = Mapper.MapToSubjectsSearchResult(ListResponse([Minimal("A"), Minimal("B")]));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new SubjectsSearchResult([new Subject("A"), new Subject("B")], RequestId, RequestInstant));
    }

    [Fact]
    public void MapToSubjectsSearchResult_Subjects_ReturnsAReadOnlyList()
    {
        // Act
        var result = Mapper.MapToSubjectsSearchResult(ListResponse([Minimal("A")]));

        // Assert
        result.Value.Subjects.ShouldBeOfType<System.Collections.ObjectModel.ReadOnlyCollection<Subject>>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MapToSubjectsSearchResult_NoSubjects_ReturnsNotFoundWithTheProof(bool nullList)
    {
        // Act
        var result = Mapper.MapToSubjectsSearchResult(ListResponse(nullList ? null : []));

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
        error.Code.ShouldBe(NotFoundError.ErrorCode);
        error.Message.ShouldBe("MF White List API has no subject on the list with the account number on the requested date.");
        error.RequestId.ShouldBe(RequestId);
        error.RequestDateTime.ShouldBe(RequestInstant);
    }

    [Fact]
    public void MapToSubjectsSearchResult_NullSubject_ThrowsMissingSubject()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToSubjectsSearchResult(ListResponse([Minimal("A"), null])))
            .Message.ShouldBe("MF White List API response is missing 'subjects[]'.");
    }

    [Fact]
    public void MapToSubjectsSearchResult_NullDto_ThrowsMissingResult()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToSubjectsSearchResult(null!))
            .Message.ShouldBe("MF White List API response is missing 'result'.");
    }

    [Fact]
    public void MapToSubjectsSearchResult_NullResult_ThrowsMissingResult()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToSubjectsSearchResult(new EntityListResponseDto { Result = null! }))
            .Message.ShouldBe("MF White List API response is missing 'result'.");
    }

    [Fact]
    public void MapToSubjectsSearchResult_NullRequestId_ThrowsMissingRequestId()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToSubjectsSearchResult(ListResponse([], requestId: null!)))
            .Message.ShouldBe("MF White List API response is missing 'requestId'.");
    }

    [Fact]
    public void MapToSubjectsSearchResult_NullRequestDateTime_ThrowsMissingRequestDateTime()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToSubjectsSearchResult(ListResponse([], requestDateTime: null!)))
            .Message.ShouldBe("MF White List API response is missing 'requestDateTime'.");
    }
}

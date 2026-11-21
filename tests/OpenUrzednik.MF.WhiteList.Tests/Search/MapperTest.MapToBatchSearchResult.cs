using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Dto;
using OpenUrzednik.MF.WhiteList.Search;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Search;

public sealed partial class MapperTest
{
    [Fact]
    public void MapToBatchSearchResult_Entries_ReturnsThemInOrderWithTheProof()
    {
        // Act
        var result = Mapper.MapToBatchSearchResult(BatchResponse([Entry("5260250274", Minimal("A")), Entry("3245174504", Minimal("B"))]), 2);

        // Assert
        result.ShouldBe(new BatchSearchResult(
        [
            new BatchSearchEntry("5260250274", [new Subject("A")], null),
            new BatchSearchEntry("3245174504", [new Subject("B")], null),
        ], RequestId, RequestInstant));
        result.Entries.ShouldBeOfType<System.Collections.ObjectModel.ReadOnlyCollection<BatchSearchEntry>>();
    }

    [Fact]
    public void MapToBatchSearchResult_NotOnTheList_UsesTheBatchProof()
    {
        // Act
        var result = Mapper.MapToBatchSearchResult(BatchResponse([Entry("5260250274")]), 1);

        // Assert
        var error = result.Entries.ShouldHaveSingleItem().Error.ShouldBeOfType<WhiteListNotFoundError>();
        error.RequestId.ShouldBe(RequestId);
        error.RequestDateTime.ShouldBe(RequestInstant);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void MapToBatchSearchResult_AnotherNumberOfEntries_ThrowsInvalidPayload(int count)
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToBatchSearchResult(BatchResponse([Entry("5260250274"), Entry("3245174504")]), count))
            .Message.ShouldBe("MF White List API response has another number of 'entries' than the identifiers sent.");
    }

    [Fact]
    public void MapToBatchSearchResult_NullEntries_ThrowsMissingEntries()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToBatchSearchResult(BatchResponse(null), 1))
            .Message.ShouldBe("MF White List API response is missing 'entries'.");
    }

    [Fact]
    public void MapToBatchSearchResult_NullEntry_ThrowsMissingEntry()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToBatchSearchResult(BatchResponse([Entry("5260250274"), null]), 2))
            .Message.ShouldBe("MF White List API response is missing 'entries[]'.");
    }

    [Fact]
    public void MapToBatchSearchResult_NullDto_ThrowsMissingResult()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToBatchSearchResult(null!, 1))
            .Message.ShouldBe("MF White List API response is missing 'result'.");
    }

    [Fact]
    public void MapToBatchSearchResult_NullResult_ThrowsMissingResult()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToBatchSearchResult(new EntryListResponseDto { Result = null! }, 1))
            .Message.ShouldBe("MF White List API response is missing 'result'.");
    }

    [Fact]
    public void MapToBatchSearchResult_NullRequestId_ThrowsMissingRequestId()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToBatchSearchResult(BatchResponse([Entry("5260250274")], requestId: null!), 1))
            .Message.ShouldBe("MF White List API response is missing 'requestId'.");
    }

    [Fact]
    public void MapToBatchSearchResult_NullRequestDateTime_ThrowsMissingRequestDateTime()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToBatchSearchResult(BatchResponse([Entry("5260250274")], requestDateTime: null!), 1))
            .Message.ShouldBe("MF White List API response is missing 'requestDateTime'.");
    }
}

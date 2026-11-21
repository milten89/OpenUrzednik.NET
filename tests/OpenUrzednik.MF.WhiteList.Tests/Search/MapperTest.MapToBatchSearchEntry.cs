using OpenUrzednik.Core.Errors;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Dto;
using OpenUrzednik.MF.WhiteList.Search;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Search;

public sealed partial class MapperTest
{
    [Fact]
    public void MapToBatchSearchEntry_Subjects_ReturnsThemInOrderWithoutError()
    {
        // Act
        var result = Mapper.MapToBatchSearchEntry(Entry("70506405335016096312945164", Minimal("A"), Minimal("B")), RequestId, RequestInstant);

        // Assert
        result.ShouldBe(new BatchSearchEntry("70506405335016096312945164", [new Subject("A"), new Subject("B")], null));
        result.IsSuccess.ShouldBeTrue();
        result.Subjects.ShouldBeOfType<System.Collections.ObjectModel.ReadOnlyCollection<Subject>>();
    }

    [Fact]
    public void MapToBatchSearchEntry_NoSubjects_ReturnsNotFoundWithTheProof()
    {
        // Act
        var result = Mapper.MapToBatchSearchEntry(Entry("5260250274"), RequestId, RequestInstant);

        // Assert
        result.Identifier.ShouldBe("5260250274");
        result.Subjects.ShouldBeEmpty();
        result.IsSuccess.ShouldBeFalse();
        var error = result.Error.ShouldBeOfType<WhiteListNotFoundError>();
        error.Code.ShouldBe(NotFoundError.ErrorCode);
        error.Message.ShouldBe("MF White List API has no subject on the list for the identifier on the requested date.");
        error.RequestId.ShouldBe(RequestId);
        error.RequestDateTime.ShouldBe(RequestInstant);
    }

    [Fact]
    public void MapToBatchSearchEntry_Error_ReturnsQueryRejectedWithTheCodeAndMessage()
    {
        // Act
        var result = Mapper.MapToBatchSearchEntry(Rejected("Nieprawidłowy NIP.", "WL-115"), RequestId, RequestInstant);

        // Assert
        result.Identifier.ShouldBe("1234567890");
        result.Subjects.ShouldBeEmpty();
        var error = result.Error.ShouldBeOfType<WhiteListQueryRejectedError>();
        error.Message.ShouldBe("MF White List API rejected the identifier: WL-115 Nieprawidłowy NIP.");
        error.WhiteListCode.ShouldBe("WL-115");
    }

    [Fact]
    public void MapToBatchSearchEntry_ErrorEchoingTheIdentifier_MasksIt()
    {
        // Act
        var result = Mapper.MapToBatchSearchEntry(Rejected("Nieprawidłowy NIP 736-812-54-16.", "WL-115"), RequestId, RequestInstant);

        // Assert
        result.Error.ShouldNotBeNull().Message.ShouldBe("MF White List API rejected the identifier: WL-115 Nieprawidłowy NIP ***-***-54-16.");
    }

    [Fact]
    public void MapToBatchSearchEntry_ErrorWithAnUndocumentedCode_DropsTheCode()
    {
        // Act
        var result = Mapper.MapToBatchSearchEntry(Rejected("Nieprawidłowy NIP.", "7368125416"), RequestId, RequestInstant);

        // Assert
        var error = result.Error.ShouldBeOfType<WhiteListQueryRejectedError>();
        error.WhiteListCode.ShouldBeNull();
        error.Message.ShouldBe("MF White List API rejected the identifier: Nieprawidłowy NIP.");
    }

    [Fact]
    public void MapToBatchSearchEntry_EmptyError_ReturnsQueryRejectedWithoutDetail()
    {
        // Act
        var result = Mapper.MapToBatchSearchEntry(Rejected(null, null), RequestId, RequestInstant);

        // Assert
        var error = result.Error.ShouldBeOfType<WhiteListQueryRejectedError>();
        error.WhiteListCode.ShouldBeNull();
        error.Message.ShouldBe("MF White List API rejected the identifier.");
    }

    [Fact]
    public void MapToBatchSearchEntry_ErrorAndSubjects_ReturnsTheError()
    {
        // Arrange
        var dto = new EntryDto { Identifier = "1234567890", Subjects = [Minimal("A")], Error = new ErrorDto { Message = "Nieprawidłowy NIP.", Code = "WL-115" } };

        // Act
        var result = Mapper.MapToBatchSearchEntry(dto, RequestId, RequestInstant);

        // Assert
        result.Subjects.ShouldBeEmpty();
        result.Error.ShouldBeOfType<WhiteListQueryRejectedError>();
    }

    [Fact]
    public void MapToBatchSearchEntry_NeitherSubjectsNorError_ThrowsInvalidPayload()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToBatchSearchEntry(new EntryDto { Identifier = "5260250274" }, RequestId, RequestInstant))
            .Message.ShouldBe("MF White List API response has an entry with neither 'subjects' nor 'error'.");
    }

    [Fact]
    public void MapToBatchSearchEntry_NullIdentifier_ThrowsMissingIdentifier()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToBatchSearchEntry(Entry(null!), RequestId, RequestInstant))
            .Message.ShouldBe("MF White List API response is missing 'entries[].identifier'.");
    }

    [Fact]
    public void MapToBatchSearchEntry_NullSubject_ThrowsMissingSubject()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToBatchSearchEntry(Entry("5260250274", Minimal("A"), null), RequestId, RequestInstant))
            .Message.ShouldBe("MF White List API response is missing 'entries[].subjects[]'.");
    }

    private static EntryDto Rejected(string? message, string? code)
        => new() { Identifier = "1234567890", Error = new ErrorDto { Message = message, Code = code } };
}

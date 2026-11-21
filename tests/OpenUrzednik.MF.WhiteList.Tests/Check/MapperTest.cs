using OpenUrzednik.MF.WhiteList.Check;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Dto;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Check;

public sealed class MapperTest
{
    private const string RequestId = "wAaOW-99b2nhg";
    private const string RequestDateTime = "21-11-2026 18:14:16";
    private static readonly DateTimeOffset RequestInstant = new(2026, 11, 21, 18, 14, 16, TimeSpan.FromHours(1));

    private static EntityCheckResponseDto Response(string accountAssigned = "TAK", string requestId = RequestId, string requestDateTime = RequestDateTime)
        => new() { Result = new EntityCheckDto { AccountAssigned = accountAssigned, RequestId = requestId, RequestDateTime = requestDateTime } };

    [Theory]
    [InlineData("TAK", true)]
    [InlineData("NIE", false)]
    public void MapToAccountCheckResult_KnownVerdict_ReturnsItWithTheProof(string accountAssigned, bool expected)
    {
        // Act
        var result = Mapper.MapToAccountCheckResult(Response(accountAssigned));

        // Assert
        result.ShouldBe(new AccountCheckResult(expected, RequestId, RequestInstant));
        result.RequestDateTime.Offset.ShouldBe(TimeSpan.FromHours(1));
    }

    [Theory]
    [InlineData("tak")]
    [InlineData("Nie")]
    [InlineData("YES")]
    [InlineData("")]
    [InlineData(" TAK")]
    public void MapToAccountCheckResult_UnknownVerdict_ThrowsWithoutTheValue(string accountAssigned)
    {
        // Act & Assert
        var exception = Should.Throw<InvalidPayloadException>(() => Mapper.MapToAccountCheckResult(Response(accountAssigned)));
        exception.Message.ShouldBe("MF White List API response has an 'accountAssigned' that is neither 'TAK' nor 'NIE'.");
    }

    [Fact]
    public void MapToAccountCheckResult_NullVerdict_ThrowsMissingAccountAssigned()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToAccountCheckResult(Response(accountAssigned: null!)))
            .Message.ShouldBe("MF White List API response is missing 'accountAssigned'.");
    }

    [Fact]
    public void MapToAccountCheckResult_NullDto_ThrowsMissingResult()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToAccountCheckResult(null!))
            .Message.ShouldBe("MF White List API response is missing 'result'.");
    }

    [Fact]
    public void MapToAccountCheckResult_NullResult_ThrowsMissingResult()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToAccountCheckResult(new EntityCheckResponseDto { Result = null! }))
            .Message.ShouldBe("MF White List API response is missing 'result'.");
    }

    [Fact]
    public void MapToAccountCheckResult_NullRequestId_ThrowsMissingRequestId()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToAccountCheckResult(Response(requestId: null!)))
            .Message.ShouldBe("MF White List API response is missing 'requestId'.");
    }

    [Fact]
    public void MapToAccountCheckResult_BadRequestDateTime_Throws()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToAccountCheckResult(Response(requestDateTime: "2026-11-21 18:14:16")))
            .Message.ShouldBe("MF White List API response has a 'requestDateTime' not in the format dd-MM-yyyy HH:mm:ss.");
    }

    [Fact]
    public void MapToAccountCheckResult_UnknownVerdictWithBadRequestDateTime_ReportsTheProofFirst()
    {
        // Act & Assert: the proof is checked before the verdict is read
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToAccountCheckResult(Response("MAYBE", requestDateTime: "x")))
            .Message.ShouldContain("requestDateTime");
    }
}

using OpenUrzednik.Core.Errors;
using OpenUrzednik.MF.WhiteList.Common;
using OpenUrzednik.MF.WhiteList.Dto;
using OpenUrzednik.MF.WhiteList.Status;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Status;

public sealed class MapperTest
{
    private const string RequestId = "Oytfp-99b3aea";
    private const string ResponseDateTime = "21-11-2026 19:58:34";
    private const string RequestedDate = "02-11-2026";
    private static readonly DateTimeOffset Answered = new(2026, 11, 21, 19, 58, 34, TimeSpan.FromHours(1));

    private static StatusResponseDto Response(string? statusCode, string? requestId = RequestId, string? responseDateTime = ResponseDateTime, string? requestedDate = RequestedDate)
        => new()
        {
            Result = new StatusResultDto
            {
                Subject = new StatusSubjectDto { StatusCode = statusCode, RequestId = requestId, ResponseDateTime = responseDateTime, RequestedDate = requestedDate },
            },
        };

    [Theory]
    [InlineData("C", VatStatus.Active)]
    [InlineData("Z", VatStatus.Exempt)]
    [InlineData("N", VatStatus.NotRegistered)]
    public void MapToVatStatusResult_StatusCode_ReturnsTheStatusWithTheProofAndTheDay(string code, VatStatus expected)
    {
        // Act
        var result = Mapper.MapToVatStatusResult(Response(code));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new VatStatusResult(expected, new DateTime(2026, 11, 2), RequestId, Answered));
        result.Value.Date.Kind.ShouldBe(DateTimeKind.Unspecified);
        result.Value.RequestDateTime.Offset.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public void MapToVatStatusResult_B_ReturnsNotFoundWithTheProofAndNoNip()
    {
        // Act
        var result = Mapper.MapToVatStatusResult(Response("B", requestedDate: null));

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListNotFoundError>();
        error.Message.ShouldBe(Mapper.NotInRegisterMessage);
        error.RequestId.ShouldBe(RequestId);
        error.RequestDateTime.ShouldBe(Answered);
    }

    [Theory]
    [InlineData("I", "MF White List API rejected the query: invalid NIP.")]
    [InlineData("D", "MF White List API rejected the query: date out of range.")]
    public void MapToVatStatusResult_IOrD_ReturnsQueryRejectedWithTheCodeWithoutTheProof(string code, string message)
    {
        // Act: the proof isn't needed for a rejection
        var result = Mapper.MapToVatStatusResult(Response(code, requestId: null, responseDateTime: null, requestedDate: null));

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListQueryRejectedError>();
        error.WhiteListCode.ShouldBe(code);
        error.Message.ShouldBe(message);
        error.ShouldBeAssignableTo<BadRequestError>();
    }

    [Fact]
    public void MapToVatStatusResult_X_ReturnsServiceInactiveWithTheCodeWithoutTheProof()
    {
        // Act
        var result = Mapper.MapToVatStatusResult(Response("X", requestId: null, responseDateTime: null, requestedDate: null));

        // Assert
        var error = result.Errors.ShouldHaveSingleItem().ShouldBeOfType<WhiteListServiceInactiveError>();
        error.WhiteListCode.ShouldBe("X");
        error.Message.ShouldBe("MF White List API status service is inactive.");
        error.ShouldBeAssignableTo<ServiceUnavailableError>();
    }

    [Theory]
    [InlineData("c")]
    [InlineData("CZ")]
    [InlineData("")]
    [InlineData(" C")]
    [InlineData("Q")]
    public void MapToVatStatusResult_UnknownCode_ThrowsWithoutTheValue(string code)
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToVatStatusResult(Response(code)))
            .Message.ShouldBe("MF White List API response has a 'statusCode' that isn't one of C, Z, N, B, I, D, X.");
    }

    [Fact]
    public void MapToVatStatusResult_NullCode_ThrowsMissingStatusCode()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToVatStatusResult(Response(null)))
            .Message.ShouldBe("MF White List API response is missing 'statusCode'.");
    }

    [Fact]
    public void MapToVatStatusResult_NullDto_ThrowsMissingResult()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToVatStatusResult(null!))
            .Message.ShouldBe("MF White List API response is missing 'result'.");
    }

    [Fact]
    public void MapToVatStatusResult_NullResult_ThrowsMissingResult()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToVatStatusResult(new StatusResponseDto { Result = null }))
            .Message.ShouldBe("MF White List API response is missing 'result'.");
    }

    [Fact]
    public void MapToVatStatusResult_NullSubject_ThrowsMissingSubject()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToVatStatusResult(new StatusResponseDto { Result = new StatusResultDto { Subject = null } }))
            .Message.ShouldBe("MF White List API response is missing 'subject'.");
    }

    [Theory]
    [InlineData("C")]
    [InlineData("B")]
    public void MapToVatStatusResult_NullRequestId_ThrowsMissingRequestId(string code)
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToVatStatusResult(Response(code, requestId: null)))
            .Message.ShouldBe("MF White List API response is missing 'requestId'.");
    }

    [Theory]
    [InlineData("C")]
    [InlineData("B")]
    public void MapToVatStatusResult_NullResponseDateTime_ThrowsMissingResponseDateTime(string code)
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToVatStatusResult(Response(code, responseDateTime: null)))
            .Message.ShouldBe("MF White List API response is missing 'responseDateTime'.");
    }

    [Theory]
    [InlineData("C")]
    [InlineData("B")]
    public void MapToVatStatusResult_BadResponseDateTime_ThrowsNamingTheField(string code)
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToVatStatusResult(Response(code, responseDateTime: "2026-11-21 19:58:34")))
            .Message.ShouldBe("MF White List API response has a 'responseDateTime' not in the format dd-MM-yyyy HH:mm:ss.");
    }

    [Fact]
    public void MapToVatStatusResult_NullRequestedDate_ThrowsMissingRequestedDate()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToVatStatusResult(Response("C", requestedDate: null)))
            .Message.ShouldBe("MF White List API response is missing 'requestedDate'.");
    }

    [Fact]
    public void MapToVatStatusResult_BadRequestedDate_ThrowsNamingTheField()
    {
        // Act & Assert
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToVatStatusResult(Response("C", requestedDate: "2026-11-02")))
            .Message.ShouldBe("MF White List API response has a 'requestedDate' not in the format dd-MM-yyyy.");
    }

    [Fact]
    public void MapToVatStatusResult_StatusWithSeveralProblems_ReportsRequestIdFirst()
    {
        // Act & Assert: the proof is checked in order: requestId, responseDateTime, requestedDate
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToVatStatusResult(Response("C", requestId: null, responseDateTime: "x", requestedDate: "y")))
            .Message.ShouldContain("requestId");
        Should.Throw<InvalidPayloadException>(() => Mapper.MapToVatStatusResult(Response("C", responseDateTime: "x", requestedDate: "y")))
            .Message.ShouldContain("responseDateTime");
    }
}

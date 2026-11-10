using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.TestCommon;
using OpenUrzednik.Transport.Http;

using Shouldly;

namespace OpenUrzednik.MF.WhiteList.Tests.Errors;

// WhiteListNotFoundError: the register has no entity; it keeps the proof of the check (ADR-0023).
public partial class WhiteListErrorTest
{
    private const string RequestId = "Ab12c-3d4ef5g";

    // 14:05:09 in Warsaw on a winter day (CET).
    private static readonly DateTimeOffset RequestDateTime = new(2026, 11, 8, 14, 5, 9, TimeSpan.FromHours(1));

    [Fact]
    public void Ctor_NotFound_IsNotFoundErrorWithTheProof()
    {
        // Act
        var error = new WhiteListNotFoundError("message", RequestId, RequestDateTime);

        // Assert
        error.ShouldBeAssignableTo<NotFoundError>();
        error.GetType().IsSealed.ShouldBeTrue();
        error.Code.ShouldBe(NotFoundError.ErrorCode);
        error.Message.ShouldBe("message");
        error.RequestId.ShouldBe(RequestId);
        error.RequestDateTime.ShouldBe(RequestDateTime);
        error.ShouldNotBeAssignableTo<IWhiteListErrorCode>();
        error.ShouldNotBeAssignableTo<IOpenUrzednikHttpStatusCode>();
        error.GetHttpStatus().ShouldBeNull();
    }

    [Fact]
    public void Ctor_NotFoundWithNullRequestId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new WhiteListNotFoundError("message", null!, RequestDateTime))
            .ParamName.ShouldBe("requestId");
    }

    [Theory]
    [InlineData(1, "2026-11-08T14:05:09+01:00")]
    [InlineData(2, "2026-11-08T14:05:09+02:00")]
    public void Metadata_NotFound_HoldsTheProofWithTheOffsetAndFollowsTheMetadataRule(int offsetHours, string expected)
    {
        // Arrange
        var error = new WhiteListNotFoundError("message", RequestId, new DateTimeOffset(2026, 11, 8, 14, 5, 9, TimeSpan.FromHours(offsetHours)));

        // Act
        var metadata = error.Metadata;

        // Assert
        metadata.Count.ShouldBe(2);
        metadata[WhiteListNotFoundError.RequestIdMetadataKey].ShouldBe(RequestId);
        metadata[WhiteListNotFoundError.RequestDateTimeMetadataKey].ShouldBe(expected);
        error.ShouldFollowTheMetadataRule();
    }

    [Fact]
    public void Metadata_NotFoundWithFractionalSeconds_KeepsThem()
    {
        // Arrange
        var error = new WhiteListNotFoundError("message", RequestId, RequestDateTime.AddMilliseconds(250));

        // Act
        var value = error.Metadata[WhiteListNotFoundError.RequestDateTimeMetadataKey];

        // Assert
        value.ShouldBe("2026-11-08T14:05:09.25+01:00");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(250, 2)]
    [InlineData(0, 0)]
    public void Metadata_NotFound_WritesRequestDateTimeAsCoreWritesADateTimeOffset(int milliseconds, int offsetHours)
    {
        // Arrange
        var requestDateTime = new DateTimeOffset(2026, 11, 8, 14, 5, 9, TimeSpan.FromHours(offsetHours)).AddMilliseconds(milliseconds);
        var error = new WhiteListNotFoundError("message", RequestId, requestDateTime);
        var core = new ValidationError<DateTimeOffset>("message", "rule", "requestDateTime", requestDateTime);

        // Act
        var value = error.Metadata[WhiteListNotFoundError.RequestDateTimeMetadataKey];

        // Assert
        value.ShouldBe(core.Metadata[ValidationError.ValueMetadataKey]);
    }

    [Fact]
    public void ToException_NotFound_ReturnsNotFoundExceptionWithTheError()
    {
        // Arrange
        var error = new WhiteListNotFoundError("message", RequestId, RequestDateTime);

        // Act
        var exception = error.ToException();

        // Assert
        exception.ShouldBeOfType<NotFoundException>().Message.ShouldBe("message");
        exception.Error.ShouldBeSameAs(error);
    }
}

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public class StatusCodeMetadataTest
{
    public static TheoryData<OpenUrzednikError> ErrorsWithStatusCode => new()
    {
        new BadRequestError("message", 400),
        new UnauthorizedError("message", 403),
        new NotFoundError("message", 404),
        new RateLimitExceededError("message", TimeSpan.FromSeconds(1), 429),
        new ServiceUnavailableError("message", 503),
        new UnknownError("message", 409),
    };

    public static TheoryData<OpenUrzednikError> ErrorsWithoutStatusCode => new()
    {
        new BadRequestError("message"),
        new UnauthorizedError("message"),
        new NotFoundError("message"),
        new RateLimitExceededError("message", null),
        new ServiceUnavailableError("message"),
        new UnknownError("message"),
    };

    [Theory]
    [MemberData(nameof(ErrorsWithStatusCode))]
    public void Ctor_WithStatusCode_StoresStatusCodeInMetadata(OpenUrzednikError error)
    {
        // Assert
        error.Metadata.ContainsKey(OpenUrzednikError.StatusCodeMetadataKey).ShouldBeTrue();
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBeOfType<int>();
    }

    [Theory]
    [MemberData(nameof(ErrorsWithoutStatusCode))]
    public void Ctor_WithoutStatusCode_DoesNotStoreStatusCode(OpenUrzednikError error)
    {
        // Assert
        error.Metadata.ContainsKey(OpenUrzednikError.StatusCodeMetadataKey).ShouldBeFalse();
    }

    [Fact]
    public void BadRequestError_ToException_ReturnsBadRequestExceptionWithMessageAndCode()
    {
        // Arrange
        var error = new BadRequestError("message", 400);

        // Act
        var exception = error.ToException();

        // Assert
        var badRequest = exception.ShouldBeOfType<BadRequestException>();
        badRequest.Message.ShouldBe("message");
        badRequest.Code.ShouldBe(BadRequestError.ErrorCode);
    }
}

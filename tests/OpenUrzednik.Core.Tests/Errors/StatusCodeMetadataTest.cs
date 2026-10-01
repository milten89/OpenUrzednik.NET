using OpenUrzednik.Core.Errors;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public class StatusCodeMetadataTest
{
    public static TheoryData<OpenUrzednikError, int> ErrorsWithStatusCode => new()
    {
        { new BadRequestError("message", 400), 400 },
        { new UnauthorizedError("message", 403), 403 },
        { new NotFoundError("message", 404), 404 },
        { new RateLimitExceededError("message", TimeSpan.FromSeconds(1), 429), 429 },
        { new ServiceUnavailableError("message", 503), 503 },
        { new UnknownError("message", 409), 409 },
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
    public void Ctor_WithStatusCode_StoresStatusCodeInMetadata(OpenUrzednikError error, int expectedStatusCode)
    {
        // Assert
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe(expectedStatusCode);
    }

    [Theory]
    [MemberData(nameof(ErrorsWithoutStatusCode))]
    public void Ctor_WithoutStatusCode_DoesNotStoreStatusCode(OpenUrzednikError error)
    {
        // Assert
        error.Metadata.ContainsKey(OpenUrzednikError.StatusCodeMetadataKey).ShouldBeFalse();
    }
}

using OpenUrzednik.Core.Errors;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

// The HTTP subclasses (OpenUrzednik.Transport.Http) are tested in OpenUrzednik.Transport.Http.Tests.
public class StatusCodeMetadataTest
{
    // The obsolete constructors keep storing the status until 2.0 (ADR-0021).
#pragma warning disable CS0618
    public static TheoryData<OpenUrzednikError, int> ObsoleteErrorsWithStatusCode => new()
    {
        { new BadRequestError("message", 400), 400 },
        { new UnauthorizedError("message", 403), 403 },
        { new NotFoundError("message", 404), 404 },
        { new RequestTimeoutError("message", statusCode: 408), 408 },
        { new RateLimitExceededError("message", TimeSpan.FromSeconds(1), 429), 429 },
        { new ServiceUnavailableError("message", 503), 503 },
        { new UnknownError("message", 409), 409 },
    };

    public static TheoryData<OpenUrzednikError> ErrorsWithoutStatusCode => new()
    {
        new BadRequestError("message"),
        new UnauthorizedError("message"),
        new NotFoundError("message"),
        new RequestTimeoutError("message"),
        new RequestTimeoutError("message", TimeSpan.FromSeconds(1)),
        new RequestTimeoutError("message", TimeSpan.FromSeconds(1), new TimeoutException()),
        new RateLimitExceededError("message", null),
        new ServiceUnavailableError("message"),
        new ServiceUnavailableError("message", new InvalidOperationException()),
        new UnknownError("message"),
        new BadRequestError("message", statusCode: null),
        new UnauthorizedError("message", statusCode: null),
        new NotFoundError("message", statusCode: null),
        new RequestTimeoutError("message", statusCode: null),
        new RateLimitExceededError("message", null, statusCode: null),
        new ServiceUnavailableError("message", statusCode: null),
        new UnknownError("message", statusCode: null),
    };
#pragma warning restore CS0618

    [Theory]
    [MemberData(nameof(ObsoleteErrorsWithStatusCode))]
    public void Ctor_WithStatusCode_StoresStatusCodeInMetadata(OpenUrzednikError error, int expectedStatusCode)
    {
        // Assert
        error.Metadata[HttpErrorMetadata.StatusCodeKey].ShouldBe(expectedStatusCode);
        error.TryGetHttpStatusCode(out var statusCode).ShouldBeTrue();
        statusCode.ShouldBe(expectedStatusCode);
        error.GetHttpStatusCode().ShouldBe(expectedStatusCode);
    }

    [Theory]
    [MemberData(nameof(ErrorsWithoutStatusCode))]
    public void Ctor_WithoutStatusCode_DoesNotStoreStatusCode(OpenUrzednikError error)
    {
        // Assert
        error.Metadata.ContainsKey(HttpErrorMetadata.StatusCodeKey).ShouldBeFalse();
        error.TryGetHttpStatusCode(out var statusCode).ShouldBeFalse();
        statusCode.ShouldBe(0);
        error.GetHttpStatusCode().ShouldBeNull();
    }
}

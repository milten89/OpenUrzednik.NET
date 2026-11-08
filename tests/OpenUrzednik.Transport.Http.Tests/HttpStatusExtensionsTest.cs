using System.Net;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

using Shouldly;

namespace OpenUrzednik.Transport.Http.Tests;

public class HttpStatusExtensionsTest
{
    public static TheoryData<OpenUrzednikError> ErrorsWithoutStatus => new()
    {
        new NotFoundError("message"),
        new ValidationError("message", "rule", "name", "value"),
        new SerializationError("message"),
        new ConnectionFailedError("message"),
        new ServiceUnavailableError("message", new InvalidOperationException()),
        new MetadataOnlyError(HttpStatusExtensions.StatusCodeMetadataKey, "404"),
    };

    [Fact]
    public void StatusCodeMetadataKey_Always_IsTheOneZeroKey()
    {
        // Assert
        HttpStatusExtensions.StatusCodeMetadataKey.ShouldBe("statusCode");
    }

    [Fact]
    public void TryGetHttpStatus_HttpError_ReturnsItsStatus()
    {
        // Act
        var found = new HttpNotFoundError("message", HttpStatusCode.NotFound).TryGetHttpStatus(out var statusCode);

        // Assert
        found.ShouldBeTrue();
        statusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public void TryGetHttpStatus_ErrorImplementingOnlyTheInterface_ReadsTheInterface()
    {
        // Arrange: no metadata, so only the interface can give the status
        var error = new InterfaceOnlyError(HttpStatusCode.BadGateway);

        // Act
        var found = error.TryGetHttpStatus(out var statusCode);

        // Assert
        found.ShouldBeTrue();
        statusCode.ShouldBe(HttpStatusCode.BadGateway);
        error.Metadata.ShouldBeEmpty();
    }

    [Fact]
    public void TryGetHttpStatus_ObsoleteConstructor_FallsBackToTheMetadata()
    {
        // Arrange
#pragma warning disable CS0618
        var error = new NotFoundError("message", 404);
#pragma warning restore CS0618

        // Act
        var found = error.TryGetHttpStatus(out var statusCode);

        // Assert
        found.ShouldBeTrue();
        statusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [MemberData(nameof(ErrorsWithoutStatus))]
    public void TryGetHttpStatus_ErrorWithoutStatus_ReturnsFalse(OpenUrzednikError error)
    {
        // Act
        var found = error.TryGetHttpStatus(out var statusCode);

        // Assert
        found.ShouldBeFalse();
        ((int)statusCode).ShouldBe(0);
        error.GetHttpStatus().ShouldBeNull();
    }

    [Fact]
    public void GetHttpStatus_HttpError_ReturnsItsStatus()
    {
        // Act
        var statusCode = new HttpServiceUnavailableError("message", HttpStatusCode.GatewayTimeout).GetHttpStatus();

        // Assert
        statusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
    }

    [Fact]
    public void TryGetHttpStatus_NullError_ThrowsArgumentNullException()
    {
        // Arrange
        OpenUrzednikError error = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => error.TryGetHttpStatus(out _)).ParamName.ShouldBe("error");
    }

    [Fact]
    public void GetHttpStatus_NullError_ThrowsArgumentNullException()
    {
        // Arrange
        OpenUrzednikError error = null!;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => error.GetHttpStatus()).ParamName.ShouldBe("error");
    }

    private sealed class InterfaceOnlyError(HttpStatusCode statusCode) : OpenUrzednikError("test", "message"), IOpenUrzednikHttpStatusCode
    {
        public HttpStatusCode StatusCode { get; } = statusCode;

        protected override OpenUrzednikException CreateException() => new UnknownException(Message);
    }

    // A status key with a value that isn't an int: not an HTTP status.
    private sealed class MetadataOnlyError : OpenUrzednikError
    {
        public MetadataOnlyError(string key, object value)
            : base("test", "message")
            => AddMetadata(key, value);

        protected override OpenUrzednikException CreateException() => new UnknownException(Message);
    }
}

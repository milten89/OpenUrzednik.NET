using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public class ConnectionFailedErrorTest
{
    [Fact]
    public void Ctor_Always_HasConnectionFailedCodeAndNoStatusCode()
    {
        // Act
        var error = new ConnectionFailedError("message");

        // Assert
        error.Code.ShouldBe("connectionFailed");
        error.Metadata.ContainsKey(OpenUrzednikError.StatusCodeMetadataKey).ShouldBeFalse();
    }

    [Fact]
    public void ToException_WithException_ReturnsConnectionFailedExceptionWrappingOriginal()
    {
        // Arrange
        var original = new HttpRequestException("Connection refused");
        var error = new ConnectionFailedError("message", original);

        // Act
        var exception = error.ToException();

        // Assert
        var connectionFailedException = exception.ShouldBeOfType<ConnectionFailedException>();
        connectionFailedException.Message.ShouldBe("message");
        connectionFailedException.Code.ShouldBe(ConnectionFailedError.ErrorCode);
        connectionFailedException.InnerException.ShouldBeSameAs(original);
        connectionFailedException.Error.ShouldBeSameAs(error);
    }

    [Fact]
    public void ToException_WithoutException_ReturnsConnectionFailedExceptionWithoutInnerException()
    {
        // Arrange
        var error = new ConnectionFailedError("message");

        // Act
        var exception = error.ToException();

        // Assert
        exception.ShouldBeOfType<ConnectionFailedException>().InnerException.ShouldBeNull();
        error.Exception.ShouldBeNull();
    }
}

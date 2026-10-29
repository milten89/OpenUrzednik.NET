using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public partial class RequestTimeoutErrorTest
{
    [Fact]
    public void ToException_WithException_KeepsInnerExceptionAndCode()
    {
        // Arrange
        var original = new TaskCanceledException();
        var error = new RequestTimeoutError("message", TimeSpan.FromSeconds(5), original);

        // Act
        var exception = error.ToException();

        // Assert
        var timeoutException = exception.ShouldBeOfType<RequestTimeoutException>();
        timeoutException.InnerException.ShouldBeSameAs(original);
        timeoutException.Code.ShouldBe(RequestTimeoutError.ErrorCode);
        error.Timeout.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ToException_WithoutException_HasNoInnerException()
    {
        // Act
        var exception = new RequestTimeoutError("message").ToException();

        // Assert
        exception.ShouldBeOfType<RequestTimeoutException>().InnerException.ShouldBeNull();
    }

    [Fact]
    public void Ctor_WithStatusCode_StoresItAsMetadata()
    {
        // Act
        var error = new RequestTimeoutError("message", statusCode: 408);

        // Assert
        error.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe(408);
        error.Timeout.ShouldBeNull();
    }
}

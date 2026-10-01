using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public class NetworkErrorsTest
{
    [Fact]
    public void ServiceUnavailableError_ToException_WithException_KeepsInnerException()
    {
        // Arrange
        var original = new HttpRequestException("Connection refused");
        var error = new ServiceUnavailableError("message", exception: original);

        // Act
        var exception = error.ToException();

        // Assert
        exception.ShouldBeOfType<ServiceUnavailableException>().InnerException.ShouldBeSameAs(original);
    }

    [Fact]
    public void RequestTimeoutError_ToException_WithException_KeepsInnerExceptionAndCode()
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
    public void RequestTimeoutError_ToException_WithoutException_HasNoInnerException()
    {
        // Act
        var exception = new RequestTimeoutError("message").ToException();

        // Assert
        exception.ShouldBeOfType<RequestTimeoutException>().InnerException.ShouldBeNull();
    }
}

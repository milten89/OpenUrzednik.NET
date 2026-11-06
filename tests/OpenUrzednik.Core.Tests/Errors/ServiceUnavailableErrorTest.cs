using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public partial class ServiceUnavailableErrorTest
{
    [Fact]
    public void ToException_WithException_KeepsInnerException()
    {
        // Arrange
        var original = new HttpRequestException("Connection refused");
        var error = new ServiceUnavailableError("message", exception: original);

        // Act
        var exception = error.ToException();

        // Assert
        exception.ShouldBeOfType<ServiceUnavailableException>().InnerException.ShouldBeSameAs(original);
    }
}

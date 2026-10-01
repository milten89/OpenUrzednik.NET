using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Errors;

public class BadRequestErrorTest
{
    [Fact]
    public void ToException_ReturnsBadRequestExceptionWithMessageAndCode()
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

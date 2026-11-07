using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Exceptions;

public partial class RateLimitExceededExceptionTest
{
    [Fact]
    public void Ctor_RetryAfter_KeepsIt()
    {
        // Act
        var exception = new RateLimitExceededException("Too many requests.", TimeSpan.FromSeconds(30));

        // Assert
        exception.Code.ShouldBe(RateLimitExceededError.ErrorCode);
        exception.Message.ShouldBe("Too many requests.");
        exception.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Ctor_RetryAfterAndInnerException_KeepsBoth()
    {
        // Arrange
        var inner = new InvalidOperationException("inner");

        // Act
        var exception = new RateLimitExceededException("Too many requests.", TimeSpan.FromSeconds(30), inner);

        // Assert
        exception.Code.ShouldBe(RateLimitExceededError.ErrorCode);
        exception.RetryAfter.ShouldBe(TimeSpan.FromSeconds(30));
        exception.InnerException.ShouldBeSameAs(inner);
    }
}

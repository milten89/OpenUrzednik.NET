using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikGenericResultTest
{
    [Fact]
    public void Value_Failure_ThrowsInvalidOperationExceptionListingTheErrors()
    {
        // Arrange
        var first = new NotFoundError("No data for 2026-10-03.", 404);
        var second = new TestError("Second error");
        var result = OpenUrzednikResult.Failure<int>([first, second]);

        // Act
        var exception = Should.Throw<InvalidOperationException>(() => result.Value);

        // Assert
        exception.Message.ShouldBe($"Cannot access the value of a failed result: notFound: No data for 2026-10-03.; {second.Code}: Second error");
        exception.InnerException.ShouldBeOfType<NotFoundException>().Error.ShouldBeSameAs(first);
    }

    [Fact]
    public void Value_Default_ThrowsInvalidOperationExceptionNamingTheUnknownError()
    {
        // Act
        var exception = Should.Throw<InvalidOperationException>(() => default(OpenUrzednikResult<int>).Value);

        // Assert
        exception.Message.ShouldStartWith("Cannot access the value of a failed result: unknown: The result was not initialized");
        exception.InnerException.ShouldBeOfType<UnknownException>();
    }
}

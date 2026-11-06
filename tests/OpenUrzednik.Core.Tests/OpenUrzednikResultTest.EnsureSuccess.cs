using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikResultTest
{
    [Fact]
    public void EnsureSuccess_WhenResultIsSuccess_DoesNotThrow()
    {
        // Arrange
        var result = OpenUrzednikResult.Success();

        // Act
        result.EnsureSuccess();

        // Assert
        // No exception should be thrown
    }


    [Fact]
    public void EnsureSuccess_WhenResultIsFailureWithSingleError_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = OpenUrzednikResult.Failure(new TestError("Test error"));

        // Act & Assert
        Should.Throw<TestException>(() => result.EnsureSuccess());
    }


    [Fact]
    public void EnsureSuccess_WhenResultIsFailureWithMultipleErrors_ThrowsFirstErrorExceptionWithAllErrors()
    {
        // Arrange
        var result = OpenUrzednikResult.Failure([new TestError("Test error"), new TestError("Another test error")]);

        // Act && Assert
        var exception = Should.Throw<TestException>(() => result.EnsureSuccess());
        exception.Message.ShouldBe("Test error");
        exception.Error.ShouldBeSameAs(result.Errors[0]);
        exception.Errors.ShouldBe(result.Errors);
    }


    [Fact]
    public void EnsureSuccess_WhenResultHasValidationAndOtherErrors_ThrowsFirstErrorExceptionWithAllErrors()
    {
        // Arrange
        var result = OpenUrzednikResult.Failure([new TestError("Test error"), new ValidationError("Invalid.", "rule", "name", null)]);

        // Act
        var exception = Should.Throw<TestException>(() => result.EnsureSuccess());

        // Assert
        exception.Errors.Count.ShouldBe(2);
    }


    [Fact]
    public void EnsureSuccess_WhenResultHasMultipleErrors_ExceptionDoesNotShareResultErrorList()
    {
        // Arrange
        var result = OpenUrzednikResult.Failure([new TestError("first"), new TestError("second")]);

        // Act
        var exception = Should.Throw<TestException>(() => result.EnsureSuccess());

        // Assert
        exception.Errors.ShouldNotBeSameAs(result.Errors);
        exception.Errors.ShouldBeAssignableTo<System.Collections.ObjectModel.ReadOnlyCollection<OpenUrzednikError>>();
    }
}

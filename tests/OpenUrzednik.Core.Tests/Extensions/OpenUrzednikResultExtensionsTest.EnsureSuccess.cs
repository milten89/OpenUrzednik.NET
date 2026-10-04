using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Extensions;

public partial class OpenUrzednikResultExtensionsTest
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
    public void EnsureSuccess_WhenGenericResultIsDefault_ThrowsUnknownException()
    {
        // Arrange
        var result = default(OpenUrzednikResult<string>);

        // Act && Assert
        Should.Throw<UnknownException>(() => result.EnsureSuccess());
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
    public void EnsureSuccess_WhenGenericResultIsSuccess_DoesNotThrow()
    {
        // Arrange
        var expectedValue = 42;
        var result = OpenUrzednikResult.Success(expectedValue);

        // Act
        var value = result.EnsureSuccess();

        // Assert
        value.ShouldBe(expectedValue);
    }

    [Fact]
    public void EnsureSuccess_WhenGenericResultIsFailureWithSingleError_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = OpenUrzednikResult.Failure<int>(new TestError("Test error"));

        // Act && Assert
        Should.Throw<TestException>(() => result.EnsureSuccess());
    }

    [Fact]
    public void EnsureSuccess_WhenGenericResultIsFailureWithMultipleErrors_ThrowsFirstErrorExceptionWithAllErrors()
    {
        // Arrange
        var result = OpenUrzednikResult.Failure<int>([new TestError("Test error"), new TestError("Another test error")]);

        // Act && Assert
        var exception = Should.Throw<TestException>(() => result.EnsureSuccess());
        exception.Message.ShouldBe("Test error");
        exception.Error.ShouldBeSameAs(result.Errors[0]);
        exception.Errors.ShouldBe(result.Errors);
    }

    [Fact]
    public void EnsureSuccess_WhenResultHasMultipleValidationErrors_ThrowsOneOpenUrzednikValidationExceptionWithAllErrors()
    {
        // Arrange
        ValidationError[] errors =
        [
            new("Date must not be in the future.", "notInFuture", "date", null),
            new("Top count must be between 1 and 255.", "range", "topCount", 300),
        ];
        var result = OpenUrzednikResult.Failure<int>(errors);

        // Act
        var exception = Should.Throw<OpenUrzednikValidationException>(() => result.EnsureSuccess());

        // Assert
        exception.Code.ShouldBe(ValidationError.ErrorCode);
        exception.Error.ShouldBeSameAs(errors[0]);
        exception.Errors.ShouldBe(errors);
        exception.Message.ShouldBe("Validation failed with 2 errors: Date must not be in the future.; Top count must be between 1 and 255.");
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
    public void EnsureSuccess_WhenResultHasSingleError_ExceptionKeepsError()
    {
        // Arrange
        var error = new NotFoundError("Not found.", 404);
        var result = OpenUrzednikResult.Failure<int>(error);

        // Act
        var exception = Should.Throw<NotFoundException>(() => result.EnsureSuccess());

        // Assert
        exception.Error.ShouldBeSameAs(error);
        exception.Error!.Metadata[OpenUrzednikError.StatusCodeMetadataKey].ShouldBe(404);
        exception.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void EnsureSuccess_WhenGenericResultIsDefault_ExceptionKeepsUnknownError()
    {
        // Act
        var exception = Should.Throw<UnknownException>(() => default(OpenUrzednikResult<int>).EnsureSuccess());

        // Assert
        exception.Error.ShouldBeOfType<UnknownError>();
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

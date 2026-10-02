using System.Globalization;

using OpenUrzednik.Core.Errors;
using OpenUrzednik.Core.Exceptions;
using OpenUrzednik.Core.Extensions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Extensions;

public class OpenUrzednikResultExtensionsTest
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
    public async Task EnsureSuccessAsync_WhenResultIsSuccess_DoesNotThrow()
    {
        // Arrange
        var result = Task.FromResult(OpenUrzednikResult.Success());

        // Act
        await result.EnsureSuccessAsync();
    }

    [Fact]
    public async Task EnsureSuccessAsync_WhenResultIsFailureWithSingleError_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Task.FromResult(OpenUrzednikResult.Failure(new TestError("Test error")));

        // Act && Assert
        await Should.ThrowAsync<TestException>(async () => await result.EnsureSuccessAsync());
    }

    [Fact]
    public async Task EnsureSuccessAsync_WhenResultIsFailureWithMultipleErrors_ThrowsFirstErrorExceptionWithAllErrors()
    {
        // Arrange
        var result = Task.FromResult(OpenUrzednikResult.Failure([new TestError("Test error"), new TestError("Another test error")]));

        // Act && Assert
        var exception = await Should.ThrowAsync<TestException>(async () => await result.EnsureSuccessAsync());
        exception.Errors.Count.ShouldBe(2);
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
    public async Task EnsureSuccessAsync_WhenGenericResultIsSuccess_DoesNotThrow()
    {
        // Arrange
        var expectedValue = 42;
        var result = Task.FromResult(OpenUrzednikResult.Success(expectedValue));

        // Act
        var value = await result.EnsureSuccessAsync();

        // Assert
        value.ShouldBe(expectedValue);
    }

    [Fact]
    public async Task EnsureSuccessAsync_WhenGenericResultIsFailureWithSingleError_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Task.FromResult(OpenUrzednikResult.Failure<int>(new TestError("Test error")));

        // Act && Assert
        await Should.ThrowAsync<TestException>(async () => await result.EnsureSuccessAsync());
    }

    [Fact]
    public async Task EnsureSuccessAsync_WhenGenericResultIsFailureWithMultipleErrors_ThrowsFirstErrorExceptionWithAllErrors()
    {
        // Arrange
        var result = Task.FromResult(OpenUrzednikResult.Failure<int>([new TestError("Test error"), new TestError("Another test error")]));

        // Act && Assert
        var exception = await Should.ThrowAsync<TestException>(async () => await result.EnsureSuccessAsync());
        exception.Errors.Count.ShouldBe(2);
    }

    [Fact]
    public void EnsureSuccess_WhenResultHasMultipleValidationErrors_ThrowsOneValidationExceptionWithAllErrors()
    {
        // Arrange
        ValidationError[] errors =
        [
            new("Date must not be in the future.", "notInFuture", "date", null),
            new("Top count must be between 1 and 255.", "range", "topCount", 300),
        ];
        var result = OpenUrzednikResult.Failure<int>(errors);

        // Act
        var exception = Should.Throw<ValidationException>(() => result.EnsureSuccess());

        // Assert
        exception.Code.ShouldBe(ValidationError.ErrorCode);
        exception.Error.ShouldBeSameAs(errors[0]);
        exception.Errors.ShouldBe(errors);
        exception.Message.ShouldBe("Validation failed with 2 errors: Date must not be in the future. Top count must be between 1 and 255.");
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
    public async Task MapAsync_SuccessfulTask_MapsValue()
    {
        // Arrange
        var resultTask = Task.FromResult(OpenUrzednikResult.Success(21));

        // Act
        var result = await resultTask.MapAsync(v => v * 2);

        // Assert
        result.Value.ShouldBe(42);
    }

    [Fact]
    public async Task BindAsync_SuccessfulTaskWithSyncNext_ReturnsNextResult()
    {
        // Arrange
        var resultTask = Task.FromResult(OpenUrzednikResult.Success(21));

        // Act
        var result = await resultTask.BindAsync(v => OpenUrzednikResult.Success(v.ToString(CultureInfo.InvariantCulture)));

        // Assert
        result.Value.ShouldBe("21");
    }

    [Fact]
    public async Task BindAsync_FailedTaskWithAsyncNext_DoesNotCallNextAndKeepsErrors()
    {
        // Arrange
        var error = new TestError("Test error");
        var resultTask = Task.FromResult(OpenUrzednikResult.Failure<int>(error));
        var called = false;

        // Act
        var result = await resultTask.BindAsync(v =>
        {
            called = true;
            return Task.FromResult(OpenUrzednikResult.Success(v));
        });

        // Assert
        called.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }
}

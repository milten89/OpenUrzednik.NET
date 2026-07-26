using OpenUrzednik.Core.Errors;
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
    public void EnsureSuccess_WhenResultIsFailureWithSingleError_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = OpenUrzednikResult.Failure(new TestError("Test error"));

        // Act
        var exception = Should.Throw<TestException>(() => result.EnsureSuccess());

        // Assert
        exception.ShouldNotBeNull();
    }

    [Fact]
    public void EnsureSuccess_WhenResultIsFailureWithMultipleErrors_ThrowsAggregateException()
    {
        // Arrange
        var result = OpenUrzednikResult.Failure([new TestError("Test error"), new TestError("Another test error")]);

        // Act
        var exception = Should.Throw<AggregateException>(() => result.EnsureSuccess());

        // Assert
        exception.ShouldNotBeNull();
        exception.InnerExceptions.Count.ShouldBe(2);
    }

    [Fact]
    public async Task EnsureSuccessAsync_WhenResultIsSuccess_DoesNotThrow()
    {
        // Arrange
        var result = Task.FromResult(OpenUrzednikResult.Success());

        // Act
        await result.EnsureSuccessAsync();

        // Assert
        // No exception should be thrown
    }

    [Fact]
    public async Task EnsureSuccessAsync_WhenResultIsFailureWithSingleError_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Task.FromResult(OpenUrzednikResult.Failure(new TestError("Test error")));

        // Act
        var exception = Should.Throw<TestException>(async () => await result.EnsureSuccessAsync());

        // Assert
        exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task EnsureSuccessAsync_WhenResultIsFailureWithMultipleErrors_ThrowsAggregateException()
    {
        // Arrange
        var result = Task.FromResult(OpenUrzednikResult.Failure([new TestError("Test error"), new TestError("Another test error")]));

        // Act
        var exception = Should.Throw<AggregateException>(async () => await result.EnsureSuccessAsync());

        // Assert
        exception.ShouldNotBeNull();
        exception.InnerExceptions.Count.ShouldBe(2);
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

        // Act
        var exception = Should.Throw<TestException>(() => result.EnsureSuccess());

        // Assert
        exception.ShouldNotBeNull();
    }

    [Fact]
    public void EnsureSuccess_WhenGenericResultIsFailureWithMultipleErrors_ThrowsAggregateException()
    {
        // Arrange
        var result = OpenUrzednikResult.Failure<int>([new TestError("Test error"), new TestError("Another test error")]);

        // Act
        var exception = Should.Throw<AggregateException>(() => result.EnsureSuccess());

        // Assert
        exception.ShouldNotBeNull();
        exception.InnerExceptions.Count.ShouldBe(2);
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

        // Act
        var exception = Should.Throw<TestException>(async () => await result.EnsureSuccessAsync());

        // Assert
        exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task EnsureSuccessAsync_WhenGenericResultIsFailureWithMultipleErrors_ThrowsAggregateException()
    {
        // Arrange
        var result = Task.FromResult(OpenUrzednikResult.Failure<int>([new TestError("Test error"), new TestError("Another test error")]));

        // Act
        var exception = Should.Throw<AggregateException>(async () => await result.EnsureSuccessAsync());

        // Assert
        exception.ShouldNotBeNull();
        exception.InnerExceptions.Count.ShouldBe(2);
    }

    
}
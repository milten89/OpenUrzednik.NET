using OpenUrzednik.Core.Extensions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Extensions;

public partial class OpenUrzednikResultExtensionsTest
{
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
}

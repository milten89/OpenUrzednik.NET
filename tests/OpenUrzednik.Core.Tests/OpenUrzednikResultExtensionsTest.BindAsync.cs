using System.Globalization;

using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikResultExtensionsTest
{
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

    [Fact]
    public void BindAsync_NullTask_ThrowsArgumentNullExceptionSynchronously()
    {
        // Arrange
        Task<OpenUrzednikResult<int>> task = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = task.BindAsync(v => OpenUrzednikResult.Success(v)); })
            .ParamName.ShouldBe("resultTask");
    }

    [Fact]
    public void BindAsync_NullNext_ThrowsArgumentNullExceptionSynchronously()
    {
        // Arrange
        var task = Task.FromResult(OpenUrzednikResult.Success(1));

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = task.BindAsync((Func<int, OpenUrzednikResult<int>>)null!); })
            .ParamName.ShouldBe("next");
    }

    [Fact]
    public void BindAsync_NullTaskWithAsyncNext_ThrowsArgumentNullExceptionSynchronously()
    {
        // Arrange
        Task<OpenUrzednikResult<int>> task = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = task.BindAsync(v => Task.FromResult(OpenUrzednikResult.Success(v))); })
            .ParamName.ShouldBe("resultTask");
    }

    [Fact]
    public void BindAsync_NullAsyncNext_ThrowsArgumentNullExceptionSynchronously()
    {
        // Arrange
        var task = Task.FromResult(OpenUrzednikResult.Success(1));

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = task.BindAsync((Func<int, Task<OpenUrzednikResult<int>>>)null!); })
            .ParamName.ShouldBe("next");
    }
}

using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikResultExtensionsTest
{
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
    public async Task MapAsync_FailedTask_DoesNotCallMapAndKeepsErrors()
    {
        // Arrange
        var error = new TestError("Test error");
        var called = false;

        // Act
        var result = await Task.FromResult(OpenUrzednikResult.Failure<int>(error)).MapAsync(v =>
        {
            called = true;
            return v;
        });

        // Assert
        called.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void MapAsync_NullTask_ThrowsArgumentNullExceptionSynchronously()
    {
        // Arrange
        Task<OpenUrzednikResult<int>> task = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = task.MapAsync(v => v); })
            .ParamName.ShouldBe("resultTask");
    }

    [Fact]
    public void MapAsync_NullMap_ThrowsArgumentNullExceptionSynchronously()
    {
        // Arrange
        var task = Task.FromResult(OpenUrzednikResult.Success(1));

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => { _ = task.MapAsync<int, int>(null!); })
            .ParamName.ShouldBe("map");
    }
}

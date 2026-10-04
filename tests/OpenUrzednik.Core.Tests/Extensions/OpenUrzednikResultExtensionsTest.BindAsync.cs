using System.Globalization;

using OpenUrzednik.Core.Extensions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Extensions;

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
    public async Task BindAsync_NullNext_ThrowsArgumentNullException()
    {
        // Act && Assert
        await Should.ThrowAsync<ArgumentNullException>(() => Task.FromResult(OpenUrzednikResult.Success(1)).BindAsync((Func<int, OpenUrzednikResult<int>>)null!));
    }
}

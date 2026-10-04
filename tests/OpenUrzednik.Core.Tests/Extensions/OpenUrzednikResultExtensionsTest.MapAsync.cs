using OpenUrzednik.Core.Extensions;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests.Extensions;

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
    public async Task MapAsync_NullTask_ThrowsArgumentNullException()
    {
        // Act && Assert
        await Should.ThrowAsync<ArgumentNullException>(() => ((Task<OpenUrzednikResult<int>>)null!).MapAsync(v => v));
    }
}

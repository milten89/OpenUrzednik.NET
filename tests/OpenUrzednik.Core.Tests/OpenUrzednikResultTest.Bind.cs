using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikResultTest
{
    [Fact]
    public void Bind_Success_ReturnsNextResult()
    {
        // Act
        var bound = OpenUrzednikResult.Success().Bind(() => OpenUrzednikResult.Success(42));

        // Assert
        bound.Value.ShouldBe(42);
    }

    [Fact]
    public void Bind_Failure_DoesNotCallNextAndKeepsAllErrors()
    {
        // Arrange
        OpenUrzednikError[] errors = [new TestError("first"), new TestError("second")];
        var called = false;

        // Act
        var bound = OpenUrzednikResult.Failure(errors).Bind(() =>
        {
            called = true;
            return OpenUrzednikResult.Success(42);
        });

        // Assert
        called.ShouldBeFalse();
        bound.Errors.ShouldBe(errors);
    }

    [Fact]
    public async Task BindAsync_Success_ReturnsNextResult()
    {
        // Act
        var bound = await OpenUrzednikResult.Success().BindAsync(() => Task.FromResult(OpenUrzednikResult.Success(42)));

        // Assert
        bound.Value.ShouldBe(42);
    }

    [Fact]
    public async Task BindAsync_Failure_DoesNotCallNextAndKeepsErrors()
    {
        // Arrange
        var error = new TestError("Test error");
        var called = false;

        // Act
        var bound = await OpenUrzednikResult.Failure(error).BindAsync(() =>
        {
            called = true;
            return Task.FromResult(OpenUrzednikResult.Success(42));
        });

        // Assert
        called.ShouldBeFalse();
        bound.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }
}

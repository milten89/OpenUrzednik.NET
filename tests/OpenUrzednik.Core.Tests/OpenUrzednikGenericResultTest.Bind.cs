using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikGenericResultTest
{
    [Fact]
    public void Bind_Success_ReturnsNextResult()
    {
        // Arrange
        var error = new TestError("Test error");
        var result = OpenUrzednikResult.Success(21);

        // Act
        var bound = result.Bind(_ => OpenUrzednikResult.Failure<string>(error));

        // Assert
        bound.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void Bind_Failure_DoesNotCallNextAndKeepsAllErrors()
    {
        // Arrange
        OpenUrzednikError[] errors = [new TestError("first"), new TestError("second")];
        var result = OpenUrzednikResult.Failure<int>(errors);
        var called = false;

        // Act
        var bound = result.Bind(v =>
        {
            called = true;
            return OpenUrzednikResult.Success(v);
        });

        // Assert
        called.ShouldBeFalse();
        bound.Errors.ShouldBe(errors);
    }

    [Fact]
    public async Task BindAsync_Success_ReturnsNextResult()
    {
        // Arrange
        var result = OpenUrzednikResult.Success(21);

        // Act
        var bound = await result.BindAsync(v => Task.FromResult(OpenUrzednikResult.Success(v * 2)));

        // Assert
        bound.Value.ShouldBe(42);
    }

    [Fact]
    public async Task BindAsync_Failure_DoesNotCallNextAndKeepsErrors()
    {
        // Arrange
        var error = new TestError("Test error");
        var result = OpenUrzednikResult.Failure<int>(error);
        var called = false;

        // Act
        var bound = await result.BindAsync(v =>
        {
            called = true;
            return Task.FromResult(OpenUrzednikResult.Success(v));
        });

        // Assert
        called.ShouldBeFalse();
        bound.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }
}

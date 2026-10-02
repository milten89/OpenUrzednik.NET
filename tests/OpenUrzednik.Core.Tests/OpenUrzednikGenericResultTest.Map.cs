using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikGenericResultTest
{
    [Fact]
    public void Map_Success_ReturnsMappedValue()
    {
        // Arrange
        var result = OpenUrzednikResult.Success(21);

        // Act
        var mapped = result.Map(v => v * 2);

        // Assert
        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value.ShouldBe(42);
    }

    [Fact]
    public void Map_Failure_DoesNotCallMapAndKeepsErrors()
    {
        // Arrange
        var error = new TestError("Test error");
        var result = OpenUrzednikResult.Failure<int>(error);
        var called = false;

        // Act
        var mapped = result.Map(v =>
        {
            called = true;
            return v * 2;
        });

        // Assert
        called.ShouldBeFalse();
        mapped.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void Map_Default_ReturnsUninitializedError()
    {
        // Arrange
        var result = default(OpenUrzednikResult<int>);

        // Act
        var mapped = result.Map(v => v * 2);

        // Assert
        mapped.IsFailure.ShouldBeTrue();
        mapped.Errors.ShouldHaveSingleItem().ShouldBeOfType<UnknownError>();
    }

    [Fact]
    public void Map_NullMap_ThrowsArgumentNullException()
    {
        // Act && Assert
        Should.Throw<ArgumentNullException>(() => OpenUrzednikResult.Success(1).Map<int>(null!));
    }
}

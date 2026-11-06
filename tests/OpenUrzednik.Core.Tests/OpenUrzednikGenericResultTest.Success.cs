using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikGenericResultTest
{
    [Fact]
    public void SuccessResult_ShouldBeSuccess()
    {
        // Arrange
        var value = 42;

        // Act
        var result = OpenUrzednikResult.Success(value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe(value);
        result.Errors.Count.ShouldBe(0);
    }

    [Fact]
    public void SuccessResult_NullValue_ShouldBeSuccess()
    {
        // Act
        var result = OpenUrzednikResult.Success<string?>(null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Errors.Count.ShouldBe(0);
    }
}

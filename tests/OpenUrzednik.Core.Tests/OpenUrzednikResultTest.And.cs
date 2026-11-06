using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikResultTest
{
    [Fact]
    public void And_BothSuccessful_ReturnsSuccess()
    {
        // Act
        var result = OpenUrzednikResult.Success().And(OpenUrzednikResult.Success());

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void And_OneFailed_ReturnsItsErrors()
    {
        // Arrange
        var error = new TestError("second");

        // Act
        var result = OpenUrzednikResult.Success().And(OpenUrzednikResult.Failure(error));

        // Assert
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void And_BothFailed_ReturnsThisResultsErrorsFirst()
    {
        // Arrange
        var first = new TestError("first");
        var second = new TestError("second");

        // Act
        var result = OpenUrzednikResult.Failure(first).And(OpenUrzednikResult.Failure(second));

        // Assert
        result.Errors.ShouldBe([first, second]);
    }
}

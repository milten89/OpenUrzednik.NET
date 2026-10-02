using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikResultTest
{
    [Fact]
    public void ImplicitOperator_Error_ReturnsFailureWithError()
    {
        // Arrange
        var error = new TestError("Test error");

        // Act
        OpenUrzednikResult result = error;

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void ImplicitOperator_NullError_ThrowsArgumentNullException()
    {
        // Arrange
        OpenUrzednikError error = null!;

        // Act && Assert
        Should.Throw<ArgumentNullException>(() => (OpenUrzednikResult)error);
    }
}

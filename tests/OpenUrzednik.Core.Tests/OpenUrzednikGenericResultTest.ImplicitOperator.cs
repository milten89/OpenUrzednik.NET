using OpenUrzednik.Core.Errors;
using OpenUrzednik.TestCommon;

using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikGenericResultTest
{
    [Fact]
    public void ImplicitConversion_FailedNonGenericResult_ShouldBeFailureWithSameErrors()
    {
        // Arrange
        var error = new TestError("Test error");
        var source = OpenUrzednikResult.Failure(error);

        // Act
        OpenUrzednikResult<int> result = source;

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem().ShouldBeSameAs(error);
    }

    [Fact]
    public void ImplicitOperator_FailedNonGenericResult_KeepsAllErrors()
    {
        // Arrange
        OpenUrzednikError[] errors = [new TestError("first"), new TestError("second")];

        // Act
        OpenUrzednikResult<int> result = OpenUrzednikResult.Failure(errors);

        // Assert
        result.Errors.ShouldBe(errors);
    }
}

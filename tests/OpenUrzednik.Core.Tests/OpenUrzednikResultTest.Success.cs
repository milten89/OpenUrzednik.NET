using Shouldly;

namespace OpenUrzednik.Core.Tests;

public partial class OpenUrzednikResultTest
{
    [Fact]
    public void SuccessResult_ShouldBeSuccess()
    {
        // Act
        var result = OpenUrzednikResult.Success();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Errors.Count.ShouldBe(0);
    }
}
